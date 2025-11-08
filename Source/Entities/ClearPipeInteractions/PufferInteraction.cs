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
    class PufferInteraction : BaseInteraction
    {
        public static FieldInfo hitSpeedField = typeof(Puffer).GetField("hitSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo gotoHitSpeedMethod = typeof(Puffer).GetMethod("GotoHitSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

        public override bool AddInteraction(Entity entity)
        {
            Puffer puffer = entity as Puffer;

            if (puffer != null && !HasClearPipeInteraction(entity))
            {
                MarioClearPipeInteraction interaction = new MarioClearPipeInteraction(Vector2.Zero);

                interaction.OnPipeBlocked = PufferOnPipeBlocked;
                interaction.OnPipeEnter = PufferOnPipeEnter;
                interaction.OnPipeExit = PufferOnPipeExit;
                interaction.OnPipeUpdate = PufferOnPipeUpdate;
                interaction.CanEnterPipe = PufferCanEnterPipe;

                entity.Add(interaction);

                return true;
            }

            return false;
        }

        public static void PufferOnPipeBlocked(Entity entity, MarioClearPipeInteraction interaction)
        {

        }

        public static void PufferOnPipeEnter(Entity entity, MarioClearPipeInteraction interaction)
        {

        }

        public static void PufferOnPipeExit(Entity entity, MarioClearPipeInteraction interaction)
        {
            Puffer puffer = entity as Puffer;

            if (puffer != null)
            {
                gotoHitSpeedMethod.Invoke(puffer, new Object[] {interaction.DirectionVector * interaction.TravelSpeed});
            }
        }

        public static bool PufferCanEnterPipe(Entity entity, Direction direction)
        {
            Puffer puffer = entity as Puffer;

            if (puffer != null)
            {
                Vector2 speed = (Vector2)hitSpeedField.GetValue(puffer);

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

        public static void PufferOnPipeUpdate(Entity entity, MarioClearPipeInteraction interaction)
        {

        }
    }
}
