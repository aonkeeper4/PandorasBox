using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/timefield")]
    class TimeField : Entity
    {
        private float start;
        private float stop;

        private float startTime;
        private float stopTime;

        private float lerp;

        private static float baseTimeRate = 1f;
        private static float ourLastTimeRate = 1f;
        private static float playerTimeRate = 1f;

        private float animTimer;
        private float animRate;
        private bool render;

        private bool respectOtherTimeRates;

        private bool lingering;
        private bool playerAffected;

        private MTexture[] particleTextures;
        private TimeParticle[] particles;

        private Color tint;

        private static WeakReference<Player> targetPlayer = new WeakReference<Player>(null);
        private static WeakReference<TimeField> lingeringTarget = new WeakReference<TimeField>(null);

        private static PropertyInfo engineDeltaTimeProp = typeof(Engine).GetProperty("DeltaTime");
        private static bool hookAdded;

        public TimeField(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            lerp = 0f;

            start = data.Float("start", 0.2f);
            stop = data.Float("stop", 1f);

            startTime = data.Float("stopTime", 1.0f);
            stopTime = data.Float("startTime", 3.0f);

            animRate = data.Float("animRate", 6.0f);

            render = data.Bool("render", true);
            playerAffected = data.Bool("playerAffected", false);
            lingering = data.Bool("lingering", false);

            respectOtherTimeRates = data.Bool("respectOtherTimeRates", false);

            String rawColor = data.Attr("color", "teal");
            tint = ColorHelper.GetColor(rawColor);

            base.Add(new PlayerCollider(new Action<Player>(this.OnPlayer), null, null));
            base.Collider = (Collider)new Hitbox(data.Width, data.Height, 0, 0);

            this.particleTextures = new MTexture[4]
            {
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(24, 0, 8, 8, null),
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(0, 0, 8, 8, null),
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(8, 0, 8, 8, null),
                GFX.Game["objects/pandorasBox/timefields/particles"].GetSubtexture(16, 0, 8, 8, null)
            };
        }

        public static void PlayerUpdateHook(On.Celeste.Player.orig_Update orig, Player player)
        {
            float dt = Engine.DeltaTime;
            engineDeltaTimeProp.SetValue(null, Engine.RawDeltaTime * Engine.TimeRateB * baseTimeRate * playerTimeRate, null);

            orig(player);

            engineDeltaTimeProp.SetValue(null, dt, null);
        }

        public void OnPlayer(Player player)
        {
            targetPlayer.SetTarget(player);
            lingeringTarget.SetTarget(this);
        }

        private Boolean PlayerInside()
        {
            TimeField field = GetValueOrNull(lingeringTarget);
            Player player = GetValueOrNull(targetPlayer);

            if (player == null)
            {
                return false;
            }

            if (lingering)
            {
                return this == field || this.CollideCheck(player);
            }
            else
            {
                return this.CollideCheck(player);
            }
        }

        private Boolean PlayerMoving()
        {
            Player player = GetValueOrNull(targetPlayer);

            if (player == null)
            {
                return false;
            }

            return player.Speed != Vector2.Zero;
        }

        public override void Update()
        {
            AddHook();

            animTimer += animRate * Engine.DeltaTime;
            UpdateTimeRate();

            base.Update();
        }

        private void UpdateTimeRate()
        {
            TimeField field = GetValueOrNull(lingeringTarget);
            Player player = GetValueOrNull(targetPlayer);

            bool playerInside = PlayerInside();

            if (field?.respectOtherTimeRates == true && (Engine.TimeRate != ourLastTimeRate) && Engine.TimeRate > 0f)
            {
                baseTimeRate = Engine.TimeRate;
            }

            if (field == this && !playerInside)
            {
                Engine.TimeRate = baseTimeRate;
            }

            if (field == this && playerInside && targetPlayer != null && !player.Dead)
            {
                // The lerp updates should be in realtime, not ingame time
                float dt = Engine.RawDeltaTime * baseTimeRate;
                float delta = PlayerMoving() ? dt / startTime : -dt / stopTime;

                lerp = Calc.Clamp(lerp + delta, 0, 1);
                Engine.TimeRate = (stop + (start - stop) * lerp);

                if (field.playerAffected)
                {
                    playerTimeRate = Engine.TimeRate;
                }
            }

            if (player != null && player.Dead)
            {
                Engine.TimeRate = baseTimeRate;
            }

            ourLastTimeRate = Engine.TimeRate;
        }

        private void Setup()
        {
            particles = new TimeParticle[(int)(this.Width / 8.0 * (this.Height / 8.0) * 0.7)];
            for (int index = 0; index < particles.Length; index++)
            {
                particles[index].Position = new Vector2(Calc.Random.NextFloat(this.Width), Calc.Random.NextFloat(this.Height));
                particles[index].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
                particles[index].TimeOffset = Calc.Random.NextFloat();
                particles[index].Scale = Calc.Random.NextFloat();
                particles[index].Color = tint * particles[index].Scale;
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            AddHook();

            Setup();
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            RemoveHook();
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);

            RemoveHook();
        }

        public override void Render()
        {
            if (!render)
            {
                // Excuse me, but I spent some time copy paste modifying space jam...
                // And you decide to not render it?!
                // Well, suit yourself...

                return;
            }

            Camera camera = this.SceneAs<Level>().Camera;
            Vector2 position = camera.Position;

            // Entity is offscreen, no need to render
            if (this.Right < camera.Left || this.Left > camera.Right || this.Bottom < camera.Top || this.Top > camera.Bottom)
            {
                return;
            }

            MTexture particleTexture;
            for (int index = 0; index < particles.Length; index++)
            {
                TimeParticle particle = particles[index];

                int layer = particle.Layer;
                Vector2 targetPosition = PutInside(particle.Position + position * (float)(0.3 + 0.25 * layer));

                switch (layer)
                {
                    case 0:
                        particleTexture = particleTextures[3 - Math.Abs((int)((particle.TimeOffset * 4.0 + animTimer) % 4.0))];
                        break;

                    case 1:
                        particleTexture = particleTextures[1 + Math.Abs((int)((particle.TimeOffset * 2.0 + animTimer) % 2.0))];
                        break;

                    default:
                        particleTexture = particleTextures[2];
                        break;
                }

                if (targetPosition.X >= this.X + 3.0 && targetPosition.Y >= this.Y + 3.0 && targetPosition.X < this.Right - 3.0 && targetPosition.Y < this.Bottom - 3.0)
                {
                    particleTexture.DrawCentered(targetPosition, particle.Color);
                }
            }

            base.Render();
        }

        private struct TimeParticle
        {
            public Vector2 Position;
            public int Layer;
            public Color Color;
            public float Scale;
            public float TimeOffset;
        }

        private Vector2 PutInside(Vector2 pos)
        {
            while (pos.X < this.X)
                pos.X += this.Width;

            while (pos.X > this.X + this.Width)
                pos.X -= this.Width;

            while (pos.Y < this.Y)
                pos.Y += this.Height;

            while (pos.Y > this.Y + this.Height)
                pos.Y -= this.Height;

            return pos;
        }

        public static void AddHook()
        {
            if (!hookAdded)
            {
                On.Celeste.Player.Update += PlayerUpdateHook;
                hookAdded = true;

                baseTimeRate = Engine.TimeRate;
            }
        }

        public static void RemoveHook()
        {
            if (hookAdded)
            {
                On.Celeste.Player.Update -= PlayerUpdateHook;
                hookAdded = false;

                baseTimeRate = 1f;
                playerTimeRate = 1f;

                Engine.TimeRate = baseTimeRate;
            }
        }

        public static T GetValueOrNull<T>(WeakReference<T> weakRef) where T : class {
            T value;

            if (weakRef != null && weakRef.TryGetTarget(out value))
            {
                return value;
            }

            return default(T);
        }

        public static void Load()
        {

        }

        public static void Unload()
        {
            RemoveHook();
        }
    }
}