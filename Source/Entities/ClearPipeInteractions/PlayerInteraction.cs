using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

using static Celeste.Mod.PandorasBox.MarioClearPipeHelper;

namespace Celeste.Mod.PandorasBox.Entities.ClearPipeInteractions
{
    class PlayerInteraction : BaseInteraction
    {
        private static FieldInfo playerDashCooldownTimerField = typeof(Player).GetField("dashCooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo playerJumpGraceTimerField = typeof(Player).GetField("jumpGraceTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        public override void Load()
        {
            base.Load();

            On.Celeste.Player.Added += Player_Added;
            On.Celeste.Player.UpdateCarry += Player_UpdateCarry;
            On.Celeste.Player.OnCollideV += Player_OnCollideV;
            On.Celeste.Player.OnCollideH += Player_OnCollideH;
        }

        public override void Unload()
        {
            base.Unload();

            On.Celeste.Player.Added -= Player_Added;
            On.Celeste.Player.UpdateCarry -= Player_UpdateCarry;
            On.Celeste.Player.OnCollideV -= Player_OnCollideV;
            On.Celeste.Player.OnCollideH -= Player_OnCollideH;
        }

        private static bool canPlayerDashIntoPipe(Player player, Direction pipeDirection)
        {
            bool startDash = (Input.CrouchDashPressed || Input.DashPressed) && player.CanDash;

            // Make sure the player is actually still DashAttacking and not at the end of it
            // Otherwise the player can enter pipes in weird ways
            if (startDash || player.DashAttacking)
            {
                Vector2 dashDir = startDash ? Input.GetAimVector(player.Facing) : player.DashDir;

                // Player might have dashed into a wall, check with their aim instead
                if (player.DashAttacking && dashDir == Vector2.Zero)
                {
                    dashDir = Input.GetAimVector(player.Facing);
                }

                switch (pipeDirection)
                {
                    case Direction.Up:
                        return dashDir.Y > 0;

                    case Direction.Right:
                        return dashDir.X < 0;

                    case Direction.Down:
                        return dashDir.Y < 0;

                    case Direction.Left:
                        return dashDir.X > 0;

                    default:
                        return false;
                }
            }

            return false;
        }

        public static void PlayerOnPipeBlocked(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && !player.Dead)
            {
                player.Die(Vector2.Zero);
            }
        }

        public static void PlayerOnPipeEnter(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && !player.Dead)
            {
                if (player.StateMachine.State != Player.StRedDash)
                {
                    player.StateMachine.State = Player.StDummy;
                }

                player.StateMachine.Locked = true;
                player.DummyGravity = false;
                player.DummyAutoAnimate = false;
                player.ForceCameraUpdate = true;
                player.Speed = Vector2.Zero;

                player.Sprite.Play("spin");

                if (!player.Inventory.NoRefills)
                {
                    player.RefillDash();
                }
            }
        }

        public static void PlayerOnPipeExit(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && !player.Dead && interaction != null)
            {
                player.StateMachine.Locked = false;
                player.DummyGravity = true;
                player.DummyAutoAnimate = true;
                player.ForceCameraUpdate = false;

                if (player.StateMachine.State != Player.StRedDash)
                {
                    player.StateMachine.State = Player.StNormal;
                    playerDashCooldownTimerField.SetValue(player, 0f);
                }

                player.Speed = interaction.DirectionVector * interaction.CurrentClearPipe.TransportSpeed;

                bool redBooster = player.StateMachine.State == Player.StRedDash;

                if (Math.Abs(player.Speed.X) > 0.707)
                {
                    bool inputTowardsPipe = (player.Speed.X < 0 && Input.MoveX > 0 || player.Speed.X > 0 && Input.MoveX < 0);
                    bool wallClimbable = !ClimbBlocker.Check(player.Scene, player, player.Position + Vector2.UnitX * 3f * Math.Sign(Input.MoveX));

                    if (interaction.CurrentClearPipe.HasPipeSolids && inputTowardsPipe && !redBooster && wallClimbable && Input.GrabCheck)
                    {
                        player.Speed = Vector2.Zero;
                    }
                }

                if (!redBooster && Input.Jump.Pressed)
                {
                    player.Jump();
                } 

                if (player.StateMachine.State == Player.StRedDash)
                {
                    player.DashDir = player.Speed.SafeNormalize();
                }

                playerJumpGraceTimerField.SetValue(player, 0.1f);

                player.RefillStamina();
            }
        }

        public static bool PlayerCanEnterPipe(Entity entity, Direction direction)
        {
            Player player = entity as Player;

            if (player.Holding != null)
            {
                return false;
            }

            if (player.OnGround())
            {
                // If the player is visually ducking or pushing up against a solid
                bool canDuckInto = player.Sprite.CurrentAnimationID == "duck" && direction == Direction.Up;
                bool canPushInto = player.Sprite.CurrentAnimationID == "push" && (direction == Direction.Left || direction == Direction.Right);
                bool holdingDown = Input.MoveY > 0;
                bool holdingHorizontally = Input.MoveX != 0;
                bool dashAttacking = player.DashAttacking;

                if ((canDuckInto && holdingDown && !dashAttacking) || (canPushInto && holdingHorizontally && !dashAttacking))
                {
                    return true;
                }
            }
            else
            {
                // Player holds up near a downwards facing pipe
                if (Input.MoveY < 0 && direction == Direction.Down && player.Speed.Y < 0)
                {
                    return true;
                }
            }

            return canPlayerDashIntoPipe(player, direction);
        }

        public static void PlayerOnPipeUpdate(Entity entity, MarioClearPipeInteraction interaction)
        {
            Player player = entity as Player;

            if (player != null && player.Dead)
            {
                interaction.ExitEarly = true;
            }
        }

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            if (!HasClearPipeInteraction(self))
            {
                // TODO - Gravity helper support
                MarioClearPipeInteraction interaction = new MarioClearPipeInteraction(new Vector2(0f, 9f));

                interaction.OnPipeBlocked = PlayerOnPipeBlocked;
                interaction.OnPipeEnter = PlayerOnPipeEnter;
                interaction.OnPipeExit = PlayerOnPipeExit;
                interaction.CanEnterPipe = PlayerCanEnterPipe;
                interaction.OnPipeUpdate = PlayerOnPipeUpdate;

                self.Add(interaction);
            }

            orig(self, scene);
        }

        private void Player_UpdateCarry(On.Celeste.Player.orig_UpdateCarry orig, Player self)
        {
            // Player UpdateCarry happens before the collision check with triggers and player colliders
            // Updating the speed here makes exiting into spikes work as expected

            orig(self);

            MarioClearPipeInteraction interaction = self.Get<MarioClearPipeInteraction>();
            bool inClearPipe = interaction != null && interaction.CurrentClearPipe != null;

            if (inClearPipe)
            {
                self.Speed = interaction.DirectionVector * interaction.TravelSpeed;
            }
        }

        private void Player_OnCollideV(On.Celeste.Player.orig_OnCollideV orig, Player self, CollisionData data)
        {
            // Prevent dust particles / footstep sounds

            MarioClearPipeInteraction interaction = self.Get<MarioClearPipeInteraction>();
            bool inClearPipe = interaction != null && interaction.CurrentClearPipe != null;

            if (!inClearPipe)
            {
                orig(self, data);
            }
        }

        private void Player_OnCollideH(On.Celeste.Player.orig_OnCollideH orig, Player self, CollisionData data)
        {
            // Prevent dust particles / footstep sounds

            MarioClearPipeInteraction interaction = self.Get<MarioClearPipeInteraction>();
            bool inClearPipe = interaction != null && interaction.CurrentClearPipe != null;

            if (!inClearPipe)
            {
                orig(self, data);
            }
        }
    }
}
