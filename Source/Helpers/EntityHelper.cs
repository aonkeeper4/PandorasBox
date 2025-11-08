using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;

namespace Celeste.Mod.PandorasBox
{
    class EntityHelper
    {
        public static Entity GetClosestEntity(Entity to, List<Entity> targets)
        {
            float bestDistance = float.PositiveInfinity;
            Entity bestTarget = null;

            foreach (Entity target in targets)
            {
                float dist = (target.Position - to.Position).LengthSquared();

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }
    }
}
