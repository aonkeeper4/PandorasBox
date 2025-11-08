using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Collections;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/dreamDashController")]
    [Tracked]
    class DreamDashController : Entity
    {
        public bool AllowSameDirectionDash;
        public bool AllowDreamDashRedirection;
        public bool OverrideDreamDashSpeed;
        public bool OverrideColors;
        public bool NeverSlowDown;
        public bool UseEntrySpeedAngle;
        public bool BounceOnCollision;
        public bool CollideStickToWalls;

        private float sameDirectionSpeedMultiplier;
        private float dreamDashSpeed;

        private Color activeBackColor;
        private Color disabledBackColor;
        private Color activeLineColor;
        private Color disabledLineColor;

        private List<List<Color>> particleLayerColors;

        private bool addedColors = false;
        private bool addedParticleColors = false;

        private static FieldInfo playerDashCooldownTimerMethod = typeof(Player).GetField("dashCooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo dreamBlockActiveBackColor = typeof(DreamBlock).GetField("activeBackColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockDisabledBackColor = typeof(DreamBlock).GetField("disabledBackColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockActiveLineColor = typeof(DreamBlock).GetField("activeLineColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockDisabledLineColor = typeof(DreamBlock).GetField("disabledLineColor", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockParticles = typeof(DreamBlock).GetField("particles", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo dreamBlockPlayerHasDreamDash = typeof(DreamBlock).GetField("playerHasDreamDash", BindingFlags.Instance | BindingFlags.NonPublic);

        // Hardcode to vanilla defaults
        // Using reflection might end up with incorrect values here with mod reloading
        private static Color activeBackColorDefault = Color.Black;
        private static Color disabledBackColorDefault = Calc.HexToColor("1f2e2d");
        private static Color activeLineColorDefault = Color.White;
        private static Color disabledLineColorDefault = Calc.HexToColor("6a8480");

        private static Type dreamBlockParticleType = typeof(DreamBlock).GetNestedType("DreamParticle", BindingFlags.NonPublic);
        private static FieldInfo dreamBlockParticleLayer = dreamBlockParticleType.GetField("Layer", BindingFlags.Instance | BindingFlags.Public);
        private static FieldInfo dreamBlockParticleColor = dreamBlockParticleType.GetField("Color", BindingFlags.Instance | BindingFlags.Public);

        private static MethodInfo playerDreamDashedIntoSolid = typeof(Player).GetMethod("DreamDashedIntoSolid", BindingFlags.Instance | BindingFlags.NonPublic);

        private static ConditionalWeakTable<Player, ValueHolder<float>> wallPlayerRotations = new ConditionalWeakTable<Player, ValueHolder<float>>();
        private static ConditionalWeakTable<Player, ValueHolder<Vector2>> wallPlayerRenderOffset = new ConditionalWeakTable<Player, ValueHolder<Vector2>>();
        private static ConditionalWeakTable<Player, ValueHolder<Vector2>> wallPlayerSpeed = new ConditionalWeakTable<Player, ValueHolder<Vector2>>();

        public DreamDashController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            AllowSameDirectionDash = data.Bool("allowSameDirectionDash", false);
            AllowDreamDashRedirection = data.Bool("allowDreamDashRedirect", true);
            OverrideDreamDashSpeed = data.Bool("overrideDreamDashSpeed", false);
            OverrideColors = data.Bool("overrideColors", false);
            NeverSlowDown = data.Bool("neverSlowDown", false);
            UseEntrySpeedAngle = data.Bool("useEntrySpeedAngle", false);
            BounceOnCollision = data.Bool("bounceOnCollision", false);
            CollideStickToWalls = data.Bool("stickOnCollision", false);

            sameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1.0f);
            dreamDashSpeed = data.Float("dreamDashSpeed", 240f);

            activeBackColor = ColorHelper.GetColor(data.Attr("activeBackColor", "Black"));
            disabledBackColor = ColorHelper.GetColor(data.Attr("disabledBackColor", "1f2e2d"));
            activeLineColor = ColorHelper.GetColor(data.Attr("activeLineColor", "White"));
            disabledLineColor = ColorHelper.GetColor(data.Attr("disabledLineColor", "6a8480"));

            particleLayerColors = new List<List<Color>> {
                ColorHelper.GetColors(data.Attr("particleLayer0Colors", "ffef11,ff00d0,08a310")),
                ColorHelper.GetColors(data.Attr("particleLayer1Colors", "5fcde4,7fb25e,e0564c")),
                ColorHelper.GetColors(data.Attr("particleLayer2Colors", "5b6ee1,CC3B3B,7daa64"))
            };

            Add(new TransitionListener
            {
                OnOutBegin = transitionOnOutBegin
            });
        }

        private bool dreamDashRedirect(Player player)
        {
            if (player.StateMachine.State == Player.StDreamDash)
            {
                playerDashCooldownTimerMethod.SetValue(player, 0f);

                if (player.CanDash)
                {
                    bool sameDirection = Input.GetAimVector() == player.DashDir;

                    if (AllowDreamDashRedirection && !sameDirection || AllowSameDirectionDash && sameDirection)
                    {
                        player.Dashes = Math.Max(0, player.Dashes - 1);

                        Audio.Play("event:/char/madeline/dreamblock_enter");

                        // Freeze game when redirecting dash
                        // Consistent with dashing in the base game
                        if (Engine.TimeRate > 0.25f)
                        {
                            Celeste.Freeze(0.05f);
                        }

                        if (sameDirection)
                        {
                            player.Speed *= sameDirectionSpeedMultiplier;
                            player.DashDir *= Math.Sign(sameDirectionSpeedMultiplier);
                        }
                        else
                        {
                            player.DashDir = Input.GetAimVector();
                            player.Speed = player.DashDir * player.Speed.Length();
                        }

                        Input.Dash.ConsumeBuffer();

                        return true;
                    }
                }
            }

            return false;
        }

        private void transitionOnOutBegin()
        {
            List<DreamDashController> controllers = Scene.Entities.FindAll<DreamDashController>();
            
            if (controllers.Count > 1)
            {
                addedColors = false;
            }
        }

        // Do a bounce check and bounce if possible
        public bool AttemptBounce(Player player)
        {
            if (BounceOnCollision || CollideStickToWalls)
            {
                Vector2 moveCheckVector = player.Speed * Engine.DeltaTime;

                player.NaiveMove(moveCheckVector);

                DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();

                if (dreamBlock == null)
                {
                    bool inSolid = (bool)playerDreamDashedIntoSolid.Invoke(player, new Object[] { });
                    if (inSolid)
                    {
                        // Move the player out of the wall properly, then bounce
                        player.NaiveMove(-moveCheckVector);

                        if (BounceOnCollision)
                        {
                            BouncePlayer(player);
                        }
                        else
                        {
                            StickPlayer(player);
                        }

                        return true;
                    }
                }

                // Make sure we undo the check movement
                player.NaiveMove(-moveCheckVector);
            }

            return false;
        }

        private bool outsideAfterMove(Player player, Vector2 offset)
        {
            player.NaiveMove(offset);
            bool outside = player.CollideFirst<DreamBlock>() == null;
            player.NaiveMove(-offset);

            return outside;
        }

        public void BouncePlayer(Player player)
        {
            Vector2 horizontalMoveCheckVector = new Vector2(player.Speed.X * Engine.DeltaTime, 0f);
            Vector2 verticalMoveCheckVector = new Vector2(0f, player.Speed.Y * Engine.DeltaTime);

            bool horizontal = outsideAfterMove(player, horizontalMoveCheckVector);
            bool vertical = outsideAfterMove(player, verticalMoveCheckVector);

            if (horizontal)
            {
                player.Speed.X *= -1;
            }
            
            if (vertical)
            {
                player.Speed.Y *= -1;
            }
        }

        public void StickPlayer(Player player)
        {
            DreamBlock dreamBlock = player.CollideFirst<DreamBlock>();

            if (dreamBlock == null)
            {
                return;
            }

            wallPlayerSpeed.AddOrUpdate(player, new ValueHolder<Vector2>(player.Speed));

            Collider playerCollider = player.Collider;
            Collider dreamBlockCollider = dreamBlock.Collider;

            Vector2 horizontalMoveCheckVector = new Vector2(player.Speed.X * Engine.DeltaTime, 0f);
            Vector2 verticalMoveCheckVector = new Vector2(0f, player.Speed.Y * Engine.DeltaTime);

            bool horizontal = outsideAfterMove(player, horizontalMoveCheckVector);
            bool vertical = outsideAfterMove(player, verticalMoveCheckVector);

            player.StateMachine.State = Player.StNormal;
            player.Ducking = true;
       
            float moveOffsetX = 0f;
            float moveOffsetY = 0f;
            float renderOffsetX = 0f;
            float renderOffsetY = 0f;
            double rotation = 0.0;

            if (horizontal)
            {
                if (player.Speed.X < 0)
                {
                    rotation = Math.PI / 2;
                    moveOffsetX = dreamBlockCollider.AbsoluteLeft - player.X + playerCollider.Width / 2.0f;
                    renderOffsetX = -playerCollider.Width / 2.0f;
                    renderOffsetY = -playerCollider.Height / 2.0f;
                }
                else
                {
                    rotation = Math.PI * 3 / 2;
                    moveOffsetX = dreamBlockCollider.AbsoluteRight - player.X - playerCollider.Width / 2.0f;
                    renderOffsetX = playerCollider.Width / 2.0f;
                    renderOffsetY = -playerCollider.Height / 2.0f;
                }
            }

            if (vertical)
            {
                if (player.Speed.Y < 0)
                {
                    rotation = Math.PI;
                    moveOffsetY = dreamBlockCollider.AbsoluteTop - player.Y + playerCollider.Height - 1;
                    renderOffsetY = -playerCollider.Height;
                }
                else
                {
                    rotation = 0.0;
                    moveOffsetY = dreamBlockCollider.AbsoluteBottom - player.Y + 1;
                }
            }

            wallPlayerRotations.AddOrUpdate(player, new ValueHolder<float>((float)rotation));
            wallPlayerRenderOffset.AddOrUpdate(player, new ValueHolder<Vector2>(new Vector2(renderOffsetX, renderOffsetY)));

            player.NaiveMove(new Vector2(moveOffsetX, moveOffsetY));
        }

        public void DreamDashStartBefore(Player player)
        {
            Vector2 stickSpeed = wallPlayerSpeed.GetOrDefault(player, new ValueHolder<Vector2>(player.Speed)).value;

            wallPlayerRenderOffset.Remove(player);
            wallPlayerRotations.Remove(player);
            wallPlayerSpeed.Remove(player);

            if (UseEntrySpeedAngle)
            {
                Vector2 entryVector = stickSpeed.SafeNormalize();
                float magnitude = stickSpeed.Length();

                player.Speed = entryVector * magnitude;
            }
        }

        public void DreamDashStartAfter(Player player, Vector2 preEnterSpeed)
        {
            Vector2 dashDirection = UseEntrySpeedAngle ? preEnterSpeed.SafeNormalize() : player.DashDir;

            if (OverrideDreamDashSpeed)
            {
                player.Speed = dashDirection * dreamDashSpeed;
            }

            if (NeverSlowDown)
            {
                if (player.Speed.LengthSquared() < preEnterSpeed.LengthSquared())
                {
                    player.Speed = dashDirection * preEnterSpeed.Length();
                }
            }
        }

        public void AddParticleColors(Scene scene)
        {
            Level level = scene as Level;

            foreach (DreamBlock dreamBlock in scene.Tracker.GetEntities<DreamBlock>())
            {
                if (level.IsInBounds(dreamBlock))
                {
                    changeDreamBlockParticleColors(dreamBlock);
                }
            }

            addedParticleColors = true;
        }

        public void AddColors()
        {
            if (OverrideColors && !addedColors)
            {
                dreamBlockActiveBackColor.SetValue(null, activeBackColor);
                dreamBlockDisabledBackColor.SetValue(null, disabledBackColor);
                dreamBlockActiveLineColor.SetValue(null, activeLineColor);
                dreamBlockDisabledLineColor.SetValue(null, disabledLineColor);

                addedColors = true;
            }
        }

        public void RemoveColors()
        {
            if (OverrideColors && addedColors)
            {
                dreamBlockActiveBackColor.SetValue(null, activeBackColorDefault);
                dreamBlockDisabledBackColor.SetValue(null, disabledBackColorDefault);
                dreamBlockActiveLineColor.SetValue(null, activeLineColorDefault);
                dreamBlockDisabledLineColor.SetValue(null, disabledLineColorDefault);

                addedColors = false;
            }
        }

        private void changeDreamBlockParticleColors(DreamBlock dreamBlock)
        {
            Array particles = dreamBlockParticles.GetValue(dreamBlock) as Array;

            if (particles != null)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    var particle = particles.GetValue(i);

                    int layer = (int)dreamBlockParticleLayer.GetValue(particle);
                    Color color = Calc.Random.Choose(particleLayerColors[layer]);

                    if (color != null)
                    {
                        dreamBlockParticleColor.SetValue(particle, color);

                        particles.SetValue(particle, i);
                    }
                }
            }
        }

        public override void Added(Scene scene)
        {
            AddColors();

            base.Added(scene);
        }

        public override void Awake(Scene scene)
        {
            if (OverrideColors && !addedParticleColors)
            {
                AddParticleColors(scene);
            }

            base.Awake(scene);
        }

        public override void Removed(Scene scene)
        {
            RemoveColors();

            base.Removed(scene);
        }

        public override void SceneEnd(Scene scene)
        {
            RemoveColors();

            base.SceneEnd(scene);
        }

        private static int Player_DreamDashUpdate(On.Celeste.Player.orig_DreamDashUpdate orig, Player self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();

            bool bounced = controller?.AttemptBounce(self) ?? false;

            if (bounced)
            {
                return self.StateMachine.State;
            }
            else
            {
                return orig(self);
            }
        }

        private static void Player_DreamDashBegin(On.Celeste.Player.orig_DreamDashBegin orig, Player self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();
            controller?.DreamDashStartBefore(self);
            Vector2 beforeSpeed = self.Speed;

            orig(self);

            controller?.DreamDashStartAfter(self, beforeSpeed);
        }

        private static void DreamBlock_Setup(On.Celeste.DreamBlock.orig_Setup orig, DreamBlock self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();
            bool playerHasDreamdash = (bool)dreamBlockPlayerHasDreamDash.GetValue(self);
            Level level = self.Scene as Level;

            orig(self);

            if (playerHasDreamdash)
            {
                if (level?.IsInBounds(self) ?? false)
                {
                    controller?.changeDreamBlockParticleColors(self);
                }
            }
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            DreamDashController controller = self.Scene.Tracker.GetEntity<DreamDashController>();

            if (controller != null)
            {
                if (Input.Dash.Pressed && Input.Aim.Value != Vector2.Zero)
                {
                    controller.dreamDashRedirect(self);
                }
            }

            Facings preOrigFacing = self.Facing;
            Vector2 preOrigScale = self.Sprite.Scale;

            orig(self);

            if (wallPlayerRotations.TryGetValue(self, out var rotationHolder))
            {
                self.Facing = preOrigFacing;
                self.Sprite.Scale = preOrigScale;

                Vector2 inputAim = Input.Aim.Value;

                if (inputAim != Vector2.Zero)
                {
                    float inputAngleOffset = (inputAim.Angle() - rotationHolder.value + MathHelper.TwoPi) % MathHelper.TwoPi;
                    Facings newFacing = self.Facing;

                    if (inputAngleOffset >= Math.PI * 0.75 && inputAngleOffset <= Math.PI * 1.25)
                    {
                        newFacing = Facings.Left;
                    }
                    else if (inputAngleOffset >= Math.PI * -0.25 && inputAngleOffset <= Math.PI * 0.25 || inputAngleOffset - MathHelper.TwoPi >= Math.PI * -0.25 && inputAngleOffset - MathHelper.TwoPi <= Math.PI * 0.25)
                    {
                        newFacing = Facings.Right;
                    }

                    if (self.Facing != newFacing)
                    {
                        self.Facing = newFacing;
                    }
                }
            }
        }

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self)
        {
            Level level = self.Scene as Level;

            float playerRotation = wallPlayerRotations.GetOrDefault(self, new ValueHolder<float>(0f)).value;
            Vector2 renderOffset = wallPlayerRenderOffset.GetOrDefault(self, new ValueHolder<Vector2>(new Vector2(0f, 0f))).value;

            if (level != null && playerRotation != 0f) {
                Camera camera = level.Camera;

                float originalAngle = camera.Angle;
                Vector2 originalCameraPosition = camera.Position;
                Vector2 originalCameraOrigin = camera.Origin;
                Vector2 originalPlayerPosition = self.Sprite.Position;
                Vector2 originalPlayerHairPosition = self.Hair.Sprite.Position;

                GameplayRenderer.End();
                camera.Angle = playerRotation;
                camera.Origin = self.Position + renderOffset - camera.Position;
                camera.Position += camera.Origin;
                self.Sprite.Position += renderOffset;
                self.Hair.MoveHairBy(renderOffset);
                GameplayRenderer.Begin();

                orig(self);

                GameplayRenderer.End();
                camera.Angle = originalAngle;
                camera.Origin = originalCameraOrigin;
                camera.Position = originalCameraPosition;
                self.Sprite.Position = originalPlayerPosition;
                self.Hair.MoveHairBy(-renderOffset);
                GameplayRenderer.Begin();
            }
            else
            {
                orig(self);
            }
        }

        public static void Load()
        {
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.Player.DreamDashBegin += Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate += Player_DreamDashUpdate;
            On.Celeste.Player.Update += Player_Update;
            //On.Celeste.DreamBlock.Setup += DreamBlock_Setup;
        }

        public static void Unload()
        {
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.Player.DreamDashBegin -= Player_DreamDashBegin;
            On.Celeste.Player.DreamDashUpdate -= Player_DreamDashUpdate;
            On.Celeste.Player.Update -= Player_Update;
            //On.Celeste.DreamBlock.Setup -= DreamBlock_Setup;
        }
    }
}
