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
    [CustomEntity("pandorasBox/propellerBox")]
    class PropellerBox : Actor
    {
        public static float BoostLaunchSpeed = -280f;
        public static float BoostLaunchThreshold = -120f;

        public static MethodInfo launchBeginInfo = typeof(Player).GetMethod("LaunchBegin", BindingFlags.Instance | BindingFlags.NonPublic);

        private float noGravityTimer;
        private float hardVerticalHitSoundCooldown;

        private Level level;

        private string texture;

        private Collision onCollideH;
        private Collision onCollideV;

        private Vector2 prevLiftSpeed;
        private Vector2 previousPosition;

        private float boostDuration;

        private Sprite flashOverlaySprite;
        private List<Sprite> chargeSprites;

        private Color flashUseColor;
        private Color flashRechargedColor;

        private InteractibleHoldable interactibleHoldable;

        public Vector2 Speed;
        public Holdable Hold;

        public int MaxCharges;
        public int Charges;

        private bool rechargeOnGround;
        private string glideMode;

        public bool HasBoosted;

        public PropellerBox(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Depth = 100;
            Collider = new Hitbox(8f, 10f, -4f, -10f);

            MaxCharges = data.Int("maxCharges", 3);
            Charges = MaxCharges;

            texture = data.Attr("texture", "default");
            flashUseColor = ColorHelper.GetColor(data.Attr("flashUseColor", "#3F437C"));
            flashRechargedColor = ColorHelper.GetColor(data.Attr("flashChargeColor", "#5A1C1C"));
            glideMode = data.Attr("glideMode", "AfterUse");
            rechargeOnGround = data.Bool("rechargeOnGround", true);

            chargeSprites = new List<Sprite>();

            for (int i = 0; i <= MaxCharges; i++)
            {
                addChargeSprite(i);
            }

            Add(flashOverlaySprite = new Sprite(GFX.Game, $"objects/pandorasBox/propellerBox/{texture}/flash_overlay"));

            flashOverlaySprite.Add("flash_overlay", "", 0.045f);
            flashOverlaySprite.Justify = new Vector2(0.5f, 1f);

            Add(Hold = new Holdable());
            Hold.PickupCollider = new Hitbox(16f, 22f, -8f, -16f);
            Hold.OnPickup = OnPickup;
            Hold.OnRelease = OnRelease;
            Hold.DangerousCheck = DangerousCheck;
            Hold.OnHitSpring = HitSpring;
            Hold.SpeedGetter = (() => Speed);
            Hold.SlowFall = glideMode == "Always";

            onCollideH = OnCollideH;
            onCollideV = OnCollideV;

            LiftSpeedGraceTime = 0.1f;

            Add(new VertexLight(base.Collider.Center, Color.White, 1f, 32, 64));
            Add(new WindMover(WindMode));

            updateChargeSpriteVisibility();

            base.Tag = Tags.TransitionUpdate;
        }

        private bool animationExists(string key)
        {
            var textures = GFX.Game.orig_GetAtlasSubtextures(key).ToArray();

            return textures.Length > 0;
        }

        private void addChargeSprite(int charge)
        {
            string spriteKey = $"{charge}_charges";
            string spritePath = $"objects/pandorasBox/propellerBox/{texture}/{spriteKey}";
            Sprite sprite;

            if (animationExists(spritePath)) {
                sprite = new Sprite(GFX.Game, spritePath);
            }
            else
            {
                sprite = new Sprite(GFX.Game, $"objects/pandorasBox/propellerBox/{texture}/default_charges");
                spriteKey = "default_charges";
            }

            sprite.AddLoop(spriteKey, "", 0.1f);
            sprite.Justify = new Vector2(0.5f, 1f);
            sprite.Play(spriteKey, true, false);

            Add(sprite);
            chargeSprites.Add(sprite);
        }

        private void updateChargeSpriteVisibility()
        {
            for (int i = 0; i <= MaxCharges; i++)
            {
                Sprite sprite = chargeSprites[i];

                sprite.Visible = i == Charges;
            }
        }

        private void changeChargeSpriteRate(float rate)
        {
            for (int i = 0; i <= MaxCharges; i++)
            {
                Sprite sprite = chargeSprites[i];

                sprite.Rate = rate;
            }
        }

        private void OnCollideV(CollisionData data)
        {
            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
            }

            if (Speed.Y > 0f)
            {
                if (hardVerticalHitSoundCooldown <= 0f)
                {
                    Audio.Play("event:/game/04_cliffside/greenbooster_end", Position);
                    hardVerticalHitSoundCooldown = 0.5f;
                }
                else
                {
                    Audio.Play("event:/game/04_cliffside/greenbooster_end", Position).setVolume(0.6f);
                }
            }

            if (Speed.Y > 160f)
            {
                ImpactParticles(data.Direction);
            }

            if (Speed.Y > 140f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch))
            {
                Speed.Y *= -0.6f;
            }
            else
            {
                Speed.Y = 0f;
            }
        }

        private void OnCollideH(CollisionData data)
        {
            Audio.Play("event:/game/04_cliffside/greenbooster_enter", Position);

            if (data.Hit is DashSwitch)
            {
                (data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
            }
            
            if (Math.Abs(Speed.X) > 100f)
            {
                ImpactParticles(data.Direction);
            }

            Speed.X *= -0.4f;
        }

        private bool DangerousCheck(HoldableCollider collider)
        {
            return !Hold.IsHeld && Speed != Vector2.Zero;
        }

        public bool HitSpring(Spring spring)
        {
            if (!Hold.IsHeld)
            {
                if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
                {
                    Speed.X *= 0.5f;
                    Speed.Y = -160f;
                    noGravityTimer = 0.15f;

                    return true;
                }

                if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = 220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;

                    return true;
                }

                if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
                {
                    MoveTowardsY(spring.CenterY + 5f, 4f);
                    Speed.X = -220f;
                    Speed.Y = -80f;
                    noGravityTimer = 0.1f;

                    return true;
                }
            }

            return false;
        }

        private void OnRelease(Vector2 force)
        {
            RemoveTag(Tags.Persistent);

            if (force.X != 0f && force.Y == 0f)
            {
                force.Y = -0.4f;
            }

            Speed = force * 200f;

            if (Speed != Vector2.Zero)
            {
                noGravityTimer = 0.1f;
            }

            interactibleHoldable?.OnHoldRelease?.Invoke(Hold);
        }

        private void OnPickup()
        {
            Speed = Vector2.Zero;

            AddTag(Tags.Persistent);

            interactibleHoldable?.OnHoldPickup?.Invoke(Hold);
        }

        public override void Update()
        {
            if (boostDuration > 0)
            {
                boostDuration -= Engine.DeltaTime;
            }

            if (hardVerticalHitSoundCooldown > 0)
            {
                hardVerticalHitSoundCooldown -= Engine.DeltaTime;
            }

            Hold.CheckAgainstColliders();

            updateVisuals();
            makeSparks();

            if (Hold.IsHeld && Hold.Holder.OnGround() || !Hold.IsHeld && OnGround())
            {
                landed();
            }

            if (Hold.IsHeld)
            {
                prevLiftSpeed = Vector2.Zero;

                interactibleHoldable?.OnHoldUpdate?.Invoke(Hold);
            }
            else
            {
                if (OnGround())
                {
                    float target = (!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f));

                    Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
                    Vector2 liftSpeed = base.LiftSpeed;

                    if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
                    {
                        Speed = prevLiftSpeed;
                        prevLiftSpeed = Vector2.Zero;
                        Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);

                        if (Speed.X != 0f && Speed.Y == 0f)
                        {
                            Speed.Y = -60f;
                        }
                        if (Speed.Y < 0f)
                        {
                            noGravityTimer = 0.15f;
                        }
                    }
                    else
                    {
                        prevLiftSpeed = liftSpeed;

                        if (liftSpeed.Y < 0f && Speed.Y < 0f)
                        {
                            Speed.Y = 0f;
                        }
                    }
                }
                else if (Hold.ShouldHaveGravity)
                {
                    float speedXMaxMove = 350f;
                    float speedYMaxMove = 800f;

                    if (Math.Abs(Speed.Y) <= 30f)
                    {
                        speedYMaxMove *= 0.5f;
                    }
                    
                    if (Speed.Y < 0f)
                    {
                        speedXMaxMove *= 0.5f;
                    }

                    Speed.X = Calc.Approach(Speed.X, 0f, speedXMaxMove * Engine.DeltaTime);

                    if (noGravityTimer > 0f)
                    {
                        noGravityTimer -= Engine.DeltaTime;
                    }
                    else
                    {
                        if (level.Wind.Y < 0f)
                        {
                            Speed.Y = Calc.Approach(Speed.Y, 0f, speedYMaxMove * Engine.DeltaTime);
                        }
                        else
                        {
                            Speed.Y = Calc.Approach(Speed.Y, 30f, speedYMaxMove * Engine.DeltaTime);
                        }
                    }
                }

                previousPosition = base.ExactPosition;

                MoveH(Speed.X * Engine.DeltaTime, onCollideH);
                MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
            }

            base.Update();
        }

        private void ImpactParticles(Vector2 dir)
        {
            float direction;
            Vector2 position;
            Vector2 positionRange;

            if (dir.X > 0f)
            {
                direction = (float)Math.PI;
                position = new Vector2(base.Right, base.Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.X < 0f)
            {
                direction = 0f;
                position = new Vector2(base.Left, base.Y - 4f);
                positionRange = Vector2.UnitY * 6f;
            }
            else if (dir.Y > 0f)
            {
                direction = -(float)Math.PI / 2f;
                position = new Vector2(base.X, base.Bottom);
                positionRange = Vector2.UnitX * 6f;
            }
            else
            {
                direction = (float)Math.PI / 2f;
                position = new Vector2(base.X, base.Top);
                positionRange = Vector2.UnitX * 6f;
            }

            level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
        }

        private void WindMode(Vector2 wind)
        {
            if (!Hold.IsHeld && !(Hold.Entity as PropellerBox).OnGround())
            {
                if (wind.X != 0f)
                {
                    MoveH(wind.X * 0.3f);
                }

                if (wind.Y != 0f)
                {
                    MoveV(wind.Y * 0.7f);
                }
            }
        }

        private void makeSparks()
        {
            if (boostDuration > 0 && base.Scene.OnInterval(0.06f))
            {
                SceneAs<Level>().ParticlesFG.Emit(ClutterSwitch.P_ClutterFly, 1, base.Center, Vector2.One * 12f);
            }
        }

        private void onHoldUpdate(Holdable hold)
        {
            // Boosting during pickup animation can cut the boost of short
            // Make sure we are ready to boost
            bool pickupAnimation = hold.Holder.Sprite.CurrentAnimationID == "pickup";

            if ((Input.Dash.Pressed || Input.CrouchDash.Pressed) && !pickupAnimation && Charges > 0)
            {
                useCharge(hold);
            }
        }

        private void updateVisuals()
        {
            if (Hold.IsHeld)
            {
                if (boostDuration > 0)
                {
                    changeChargeSpriteRate(2.5f);
                }
                else if (HasBoosted)
                {
                    changeChargeSpriteRate(1.6f);
                }
                else
                {
                    changeChargeSpriteRate(1f);
                }
            }
            else if (OnGround()) 
            {
                changeChargeSpriteRate(0.4f);
            }
            else
            {
                changeChargeSpriteRate(1f);
            }
        }

        private void showChargeFlash()
        {
            flashOverlaySprite.Play("flash_overlay", true);
            flashOverlaySprite.Visible = true;
            flashOverlaySprite.OnFinish = (path) => flashOverlaySprite.Visible = false;
        }

        private void useCharge(Holdable hold)
        {
            Player player = hold.Holder;

            if (player.Speed.Y > BoostLaunchThreshold)
            {
                player.Speed.Y = BoostLaunchSpeed;
                
                if (player.StateMachine.State == Player.StPickup)
                {
                    player.StateMachine.State = Player.StNormal;
                }

                HasBoosted = true;
                boostDuration = 1.2f;
                
                if (glideMode == "AfterUse") {
                    hold.SlowFall = true;
                }

                Audio.Play("event:/game/01_forsaken_city/birdbros_thrust", Position);

                launchBeginInfo.Invoke(player, new object[] { });

                Charges--;
                flashOverlaySprite.Color = flashUseColor;
                updateChargeSpriteVisibility();
                showChargeFlash();

                Input.Dash.ConsumeBuffer();
            }
        }

        private void landed()
        {
            if (glideMode == "AfterUse")
            {
                Hold.SlowFall = false;
            }

            if (rechargeOnGround && MaxCharges > Charges)
            {
                Charges = MaxCharges;

                HasBoosted = false;

                flashOverlaySprite.Color = flashRechargedColor;
                updateChargeSpriteVisibility();
                showChargeFlash();
            }
        }

        private void onHoldPickup(Holdable hold)
        {
            Input.Dash.ConsumeBuffer();
        }

        public override void Added(Scene scene)
        {
            level = scene as Level;
            interactibleHoldable = new InteractibleHoldable(this);

            interactibleHoldable.OnHoldUpdate = onHoldUpdate;

            Add(interactibleHoldable);

            base.Added(scene);
        }
    }
}
