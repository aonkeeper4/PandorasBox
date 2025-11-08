using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox.Entities.ClearPipeInteractions
{
    class InteractionRegistry
    {
        public static HashSet<BaseInteraction> BaseInteractions = new HashSet<BaseInteraction>();

        public static void Add(BaseInteraction baseInteraction)
        {
            BaseInteractions.Add(baseInteraction);
        }

        public static void Load()
        {
            foreach (BaseInteraction baseInteraction in BaseInteractions)
            {
                baseInteraction.Load();
            }
        }

        public static void Unload()
        {
            foreach (BaseInteraction baseInteraction in BaseInteractions)
            {
                baseInteraction.Unload();
            }
        }
    }
}
