using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/airBubbles")]
    class AirBubbles : Actor
    {
        private bool oneUse;
        private float respawnTimer;

        private Sprite sprite;
        private Sprite flash;
        private Image outline;
        private Wiggler wiggler;
        private BloomPoint bloom;
        private VertexLight light;
        private SineWave sine;

        public AirBubbles(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            oneUse = data.Bool("oneUse");

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Add(new PlayerCollider(OnPlayer));

            Add(outline = new Image(GFX.Game["objects/pandorasBox/airBubbles/outline"]));
            outline.CenterOrigin();
            outline.Visible = false;

            Add(sprite = new Sprite(GFX.Game, "objects/pandorasBox/airBubbles/idle"));
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();

            Add(flash = new Sprite(GFX.Game, "objects/pandorasBox/airBubbles/flash"));
            flash.Add("flash", "", 0.05f);
            flash.OnFinish = delegate
            {
                flash.Visible = false;
            };
            flash.CenterOrigin();

            Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
            {
                sprite.Scale = (flash.Scale = Vector2.One * (1f + v * 0.2f));
            }));

            Add(new MirrorReflection());

            Add(bloom = new BloomPoint(0.8f, 16f));
            Add(light = new VertexLight(Color.White, 1f, 16, 48));

            Add(sine = new SineWave(0.6f, 0f));
            sine.Randomize();

            UpdateY();

            base.Depth = -100;
        }

        private void UpdateY()
        {
            bloom.Y = flash.Y = sprite.Y = sine.Value * 2f;
        }

        private void OnPlayer(Player player)
        {
            WaterDrowningController controller = base.Scene.Tracker.GetEntity<WaterDrowningController>();

            Audio.Play("event:/game/general/diamond_touch", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);

            if (controller != null)
            {
                controller.WaterDuration = 0f;
            }

            respawnTimer = 2.5f;
            Collidable = false;

            Add(new Coroutine(RefillRoutine(player)));
        }

        private IEnumerator RefillRoutine(Player player)
        {
            Celeste.Freeze(0.05f);

            yield return null;

            (Scene as Level).Shake();

            sprite.Visible = (flash.Visible = false);

            if (!oneUse)
            {
                outline.Visible = true;
            }
            Depth = 8999;

            yield return 0.05f;

            float angle = player.Speed.Angle();
            SlashFx.Burst(Position, angle);

            if (oneUse)
            {
                RemoveSelf();
            }
        }

        public void Respawn()
        {
            if (!Collidable)
            {
                Collidable = true;
                sprite.Visible = true;
                outline.Visible = false;

                base.Depth = -100;

                wiggler.Start();

                Audio.Play("event:/game/general/diamond_return", Position);
            }
        }

        public override void Update()
        {
            respawnTimer = Math.Max(0, respawnTimer - Engine.DeltaTime);

            if (respawnTimer <= 0)
            {
                Respawn();
            }

            UpdateY();

            light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;

            if (Scene.OnInterval(2f) && sprite.Visible)
            {
                flash.Play("flash", restart: true);
                flash.Visible = true;
            }

            base.Update();
        }
    }
}
