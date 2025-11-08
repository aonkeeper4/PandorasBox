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
    public class MarioClearPipeInteraction : Component
    {
        public delegate void OnPipeInteractionDelegate(Entity entity, MarioClearPipeInteraction interaction);
        public delegate bool CanEnterPipeDelegate(Entity entity, MarioClearPipeHelper.Direction direction);

        public OnPipeInteractionDelegate OnPipeEnter;
        public OnPipeInteractionDelegate OnPipeExit;
        public OnPipeInteractionDelegate OnPipeBlocked;
        public OnPipeInteractionDelegate OnPipeUpdate;

        public CanEnterPipeDelegate CanEnterPipe = (entity, direction) => true;

        public Vector2 PipeRenderOffset;

        public MarioClearPipe CurrentClearPipe = null;

        public MarioClearPipeHelper.Direction Direction = MarioClearPipeHelper.Direction.None;
        public Vector2 DirectionVector = Vector2.Zero;

        public bool ExitEarly = false;

        public Vector2 From = Vector2.Zero;
        public Vector2 To = Vector2.Zero;

        public float Moved = 0f;
        public float Distance = 0f;

        public float TravelSpeed = 1f;

        public MarioClearPipeInteraction(Vector2 renderOffset) : base(true, true)
        {
            PipeRenderOffset = renderOffset;
        }
    }
}
