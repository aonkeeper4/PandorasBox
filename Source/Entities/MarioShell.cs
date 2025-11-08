using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

// TODO - Clean up this file to match standard of newer entities
// TODO - Make disco mode feel good

namespace Celeste.Mod.PandorasBox
{
    [Tracked(false)]
    [CustomEntity("pandorasBox/shell")]
    class MarioShell : Actor
    {
        public float grace;
        public float springGrace;
        public float turnTime;
        public Vector2 Speed;
        public List<Color> colors;
        public float colorSpeed;
        public float timeAcc;
        public String texture;
        public int id;
        public bool dangerous;
        public bool disco;
        public bool grabbable;

        public Holdable Hold;
        public Scene scene;

        public Vector2 prevLiftspeed;

        private Sprite decorationIdle;
        private Sprite decorationMoving;
        private Sprite shellIdle;
        private Sprite shellMoving;

        private BloomPoint bloom;
        private VertexLight light;

        private Hitbox pickupMovingCollider;
        private Hitbox pickupIdleCollider;

        private Hitbox shellNotHeldCollider;
        private Hitbox shellHeldCollider;

        private Collision onCollideH;
        private Collision onCollideV;

        public static float gravity = -80f;
        public static float baseSpeed = 200f;
        public static float baseThrowHeight = 0f;
        public static float gracePush = 0.1f;
        public static float graceThrow = 0.15f;
        public static float graceBounce = 0.15f;
        public static float graceSpring = 0.15f;
        public static float turnTimeDelay = 0.3f;

        public static Dictionary<String, bool> dangerousTextures = new Dictionary<String, bool> {
            {"koopa", false},
            {"spiny", true},
            {"beetle", false},
            {"bowserjr", true}
        };

        private bool alreadyAdded
        {
            get
            {
                return Scene.Tracker.GetEntities<MarioShell>().Cast<MarioShell>().Any(other => (other.id == id && Hold.IsHeld != other.Hold.IsHeld));
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            this.scene = scene;

            if (alreadyAdded)
            {
                base.RemoveSelf();
            }
        }

        public MarioShell(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            colors = new List<Color>();
            timeAcc = 0;

            bool lights = data.Bool("lights", false);

            texture = data.Attr("texture", "koopa");
            dangerous = data.Bool("dangerous", dangerousTextures.ContainsKey(texture) && dangerousTextures[texture]);
            disco = data.Bool("disco", false);
            grabbable = data.Bool("grabbable", true);

            String rawColor = data.Attr("color", "Green");
            colorSpeed = data.Float("colorSpeed", 0.8f);

            foreach (String s in rawColor.Split(','))
            {
                colors.Add(ColorHelper.GetColor(s));
            }

            int direction = Math.Sign(data.Int("direction", 0));

            Speed = new Vector2(baseSpeed * direction, 0f);
            grace = 0f;

            prevLiftspeed = Vector2.Zero;

            pickupIdleCollider = new Hitbox(18f, 18f, -9, -9);
            pickupMovingCollider = new Hitbox(0f, 0f, 0f, 0f);

            shellHeldCollider = new Hitbox(8f, 14f, -4f, -7f);
            shellNotHeldCollider = new Hitbox(14f, 14f, -7f, -7f);

            Add(new PlayerCollider(new Action<Player>(OnPlayer)));
            Collider = shellNotHeldCollider;

            onCollideH = new Collision(OnCollideH);
            onCollideV = new Collision(OnCollideV);

            Add((Component)(decorationIdle = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/deco_idle")));
            decorationIdle.AddLoop("deco_idle", "", 0.1f);
            decorationIdle.Play("deco_idle", true, false);
            decorationIdle.CenterOrigin();

            Add((Component)(shellIdle = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/shell_idle")));
            shellIdle.AddLoop("shell_idle", "", 0.1f);
            shellIdle.Play("shell_idle", true, false);
            shellIdle.CenterOrigin();
            shellIdle.Color = colors[0];

            Add((Component)(decorationMoving = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/deco_moving")));
            decorationMoving.AddLoop("deco_moving", "", 0.1f);
            decorationMoving.Play("deco_moving", true, false);
            decorationMoving.CenterOrigin();

            Add((Component)(shellMoving = new Sprite(GFX.Game, $"objects/pandorasBox/shells/{texture}/shell_moving")));
            shellMoving.AddLoop("shell_moving", "", 0.1f);
            shellMoving.Play("shell_moving", true, false);
            shellMoving.CenterOrigin();
            shellMoving.Color = colors[0];

            Add((Component)(Hold = new Holdable()));
            Hold.PickupCollider = new Hitbox(18f, 18f, -9, -9);
            Hold.OnPickup = new Action(OnPickup);
            Hold.OnRelease = new Action<Vector2>(OnRelease);
            Hold.SpeedGetter = () => Speed;
            Hold.DangerousCheck = DangerousCheck;
            Hold.OnHitSpring = HitSpring;

            decorationMoving.Visible = shellMoving.Visible = false;

            id = data.ID;

            if (lights)
            {
                Add((Component)(bloom = new BloomPoint(1f, 16f)));
                Add((Component)(light = new VertexLight(Collider.Center, Color.White, 1f, 8, 24)));

                bloom.Visible = light.Visible = true;
            }
        }

        private bool DangerousCheck(HoldableCollider collider)
        {
            return !Hold.IsHeld && Speed != Vector2.Zero;
        }

        private void updateColors()
        {
            if (colors != null && colors.Count > 1 && colorSpeed > 0)
            {
                int index = (int)Math.Floor(timeAcc / colorSpeed % colors.Count);
                float lerp = timeAcc / colorSpeed % 1;

                Color newColor = Color.Lerp(colors[index], colors[(index + 1) % colors.Count], lerp);

                shellMoving.Color = shellIdle.Color = newColor;
            }
        }

        private void updateDiscoShell()
        {
            if (disco)
            {
                turnTime = Math.Max(0, turnTime - Engine.DeltaTime);

                if (turnTime == 0 && OnGround())
                {
                    Player player = Scene.Tracker.GetNearestEntity<Player>(Position);

                    if (player != null)
                    {
                        // Make sure we always give the shell speed if it has been landed on
                        if (Speed.X == 0)
                        {
                            Speed.X = X > player.X ? -baseSpeed : baseSpeed;
                            turnTime = turnTimeDelay;
                        }
                        else
                        {
                            if (X > player.X + 16)
                            {
                                Speed.X = -baseSpeed;
                                turnTime = turnTimeDelay;
                            }
                            else if (X < player.X - 16)
                            {
                                Speed.X = baseSpeed;
                                turnTime = turnTimeDelay;
                            }
                        }
                    }
                }
            }
        }

        public override void Update()
        {
            grace = Math.Max(0, grace - Engine.DeltaTime);
            springGrace = Math.Max(0, springGrace - Engine.DeltaTime);
            timeAcc += Engine.DeltaTime;

            updateColors();
            updateDiscoShell();

            bool useMovingVisuals = Speed.X != 0 || Get<MarioClearPipeInteraction>()?.CurrentClearPipe != null;

            decorationMoving.Visible = shellMoving.Visible = useMovingVisuals;
            decorationIdle.Visible = shellIdle.Visible = !useMovingVisuals;

            if (!Hold.IsHeld)
            {
                bool onGround = OnGround();

                Speed.Y = onGround && Speed.Y >= 0 ? 0f : Calc.Approach(Speed.Y, 200f, 400f * Engine.DeltaTime);

                MoveH(Speed.X * Engine.DeltaTime, onCollideH, null);
                MoveV(Speed.Y * Engine.DeltaTime, onCollideV, null);

                if (onGround && LiftSpeed == Vector2.Zero && prevLiftspeed != Vector2.Zero)
                {
                    Speed = prevLiftspeed;
                    Speed.X = Math.Sign(Speed.X) * baseSpeed;

                    prevLiftspeed = Vector2.Zero;
                }

                prevLiftspeed = LiftSpeed;
            }
            else
            {
                prevLiftspeed = Vector2.Zero;
            }

            if (!Hold.IsHeld && Collider == shellHeldCollider && grace == 0)
            {
                Collider = shellNotHeldCollider;

                if (CollideCheck<Solid>())
                {
                    Collider = shellHeldCollider;
                }
            }

            Hold.CheckAgainstColliders();

            Hold.PickupCollider = grabbable && Math.Abs(Speed.X) <= 10e-6 ? pickupIdleCollider : pickupMovingCollider;

            base.Update();
        }

        public bool HitSpring(Spring spring)
        {
            if (!Hold.IsHeld && springGrace <= 0)
            {
                springGrace = graceSpring;

                if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
                {
                    Speed.X = -baseSpeed;
                    Speed.Y = -140f;

                    return true;
                }
                else if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
                {
                    Speed.X = baseSpeed;
                    Speed.Y = -140f;

                    return true;

                }
                else if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
                {
                    Speed.Y = -240f;

                    return true;
                }
            }

            return false;
        }

        private void OnPlayer(Player player)
        {
            if (!Hold.IsHeld && grace == 0 && Get<MarioClearPipeInteraction>()?.CurrentClearPipe == null)
            {
                // Landing on top
                if (player.BottomCenter.Y < Center.Y)
                {
                    if (dangerous)
                    {
                        player.Die((player.Center - Position).SafeNormalize());

                        return;
                    }
                    else
                    {
                        grace = graceBounce;
                        player.Bounce(Top);

                        Speed.X = Math.Abs(Speed.X) >= 10e-6 ? 0 : ((Center.X - player.Center.X) < 0 ? -1 : 1) * baseSpeed;

                        Audio.Play("event:/game/general/thing_booped", Position).setVolume(0.5f);
                    }
                }
                else
                {
                    // Attempting a kick, only valid if shell is standing still
                    if (Speed.X == 0)
                    {
                        Speed = new Vector2(((Center.X - player.Center.X) < 0 ? -1 : 1) * baseSpeed, gravity);
                        grace = gracePush;

                        Audio.Play("event:/game/general/thing_booped", Position).setVolume(0.5f);
                    }
                    else
                    {
                        player.Die((player.Center - Position).SafeNormalize(), false, true);
                    }
                }
            }
        }

        private void OnCollideH(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }

            Audio.Play("event:/game/general/thing_booped", Position).setVolume(0.3f);
            Speed.X *= -1;
        }

        private void OnCollideV(CollisionData data)
        {
            Speed.Y = 0f;
        }

        public override bool IsRiding(Solid solid)
        {
            return base.IsRiding(solid);
        }

        private void OnPickup()
        {
            Collider = shellHeldCollider;
            AddTag(Tags.Persistent);
            Speed = new Vector2(0f, 0f);
        }

        private void OnRelease(Vector2 force)
        {
            Speed = new Vector2(Math.Sign(force.X) * baseSpeed, force.Y * baseThrowHeight);
            grace = graceThrow;

            RemoveTag(Tags.Persistent);
        }

        public override void OnSquish(CollisionData data)
        {
            if (base.TrySquishWiggle(data))
            {
                RemoveSelf();
            }
        }

        public override void Render()
        {
            base.Render();
        }
    }
}
