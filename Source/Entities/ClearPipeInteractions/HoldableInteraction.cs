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
    class HoldableInteraction : BaseInteraction
    {
        public static void HoldableOnPipeBlocked(Entity entity, MarioClearPipeInteraction interaction)
        {
            Actor actor = entity as Actor;

            if (actor != null)
            {
                // Attempt to force a squish by moving the colliding solid by 0 pixels
                Solid solid = actor.CollideFirst<Solid>(actor.Position);
                if (solid != null)
                {
                    solid.MoveHExact(0);
                }
            }
            else
            {
                entity?.Scene?.Remove(entity);
            }
        }

        public static void HoldableOnPipeEnter(Entity entity, MarioClearPipeInteraction interaction)
        {
            Holdable holdable = entity?.Get<Holdable>();

            if (holdable != null)
            {
                // Reset speeds and remove holder
                holdable.Release(Vector2.Zero);
            }
        }

        public static void HoldableOnPipeExit(Entity entity, MarioClearPipeInteraction interaction)
        {
            Holdable holdable = entity?.Get<Holdable>();

            if (holdable != null && entity.Scene != null && !holdable.IsHeld && !interaction.ExitEarly)
            {
                Vector2 speed = Vector2.Zero;

                switch (interaction.Direction)
                {
                    case Direction.Left:
                        speed = new Vector2(-1.0f, 0.1f);
                        break;

                    case Direction.Right:
                        speed = new Vector2(1.0f, -0.1f);
                        break;

                    case Direction.Up:
                        speed = new Vector2(0, -1.0f);
                        break;

                    case Direction.Down:
                        speed = new Vector2(0, 1.0f);
                        break;

                    default:
                        break;
                }

                holdable.Release(speed);
            }
        }

        public static bool HoldableCanEnterPipe(Entity entity, Direction direction)
        {
            Holdable holdable = entity?.Get<Holdable>();

            if (holdable != null && !holdable.IsHeld)
            {
                Vector2 speed = holdable?.SpeedGetter() ?? Vector2.Zero;

                switch (direction)
                {
                    case Direction.Left:
                        return speed.X > 0;

                    case Direction.Right:
                        return speed.X < 0;

                    case Direction.Up:
                        return speed.Y > 0;

                    case Direction.Down:
                        return speed.Y < 0;

                    default:
                        return false;
                }
            }

            return false;
        }

        public static void HoldableOnPipeUpdate(Entity entity, MarioClearPipeInteraction interaction)
        {
            Holdable holdable = entity.Get<Holdable>();

            interaction.ExitEarly = holdable?.IsHeld ?? false;
        }

        public override bool AddInteraction(Entity entity)
        {
            Holdable holdable = entity.Get<Holdable>();

            // Depends on SpeedGetter and OnRelease being defined
            if (holdable != null && holdable.SpeedGetter != null && holdable.OnRelease != null && !HasClearPipeInteraction(entity))
            {
                Vector2 pipeOffset = Vector2.Zero;

                if (entity.Collider != null)
                {
                    pipeOffset = -entity.Collider.Center;
                }

                MarioClearPipeInteraction interaction = new MarioClearPipeInteraction(pipeOffset);

                interaction.OnPipeBlocked = HoldableInteraction.HoldableOnPipeBlocked;
                interaction.OnPipeEnter = HoldableInteraction.HoldableOnPipeEnter;
                interaction.OnPipeExit = HoldableInteraction.HoldableOnPipeExit;
                interaction.OnPipeUpdate = HoldableInteraction.HoldableOnPipeUpdate;
                interaction.CanEnterPipe = HoldableInteraction.HoldableCanEnterPipe;

                entity.Add(interaction);

                return true;
            }

            return false;
        }
    }
}
