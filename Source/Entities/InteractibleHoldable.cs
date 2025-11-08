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
    public class InteractibleHoldable : Component
    {
        public delegate void InteractionDelegate(Holdable holdable);

        public InteractionDelegate OnHoldUpdate;
        public InteractionDelegate OnHoldPickup;
        public InteractionDelegate OnHoldRelease;

        public Entity Entity;

        public InteractibleHoldable(Entity entity) : base(true, true)
        {
            Entity = entity;
        }
    }
}
