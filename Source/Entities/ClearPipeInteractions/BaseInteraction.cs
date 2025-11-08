using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.PandorasBox.Entities.ClearPipeInteractions
{
    class BaseInteraction
    {
        public virtual void Load()
        {

        }

        public virtual void Unload()
        {

        }

        public virtual bool AddInteraction(Entity entity)
        {
            return false;
        }

        public BaseInteraction()
        {
  
        }
    }
}
