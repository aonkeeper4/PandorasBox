using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Monocle;
using System.Reflection;
using Microsoft.Xna.Framework;
using Celeste.Mod.Helpers;
using NLua;
using System.Collections;

namespace Celeste.Mod.PandorasBox
{
    public class TypeHelper
    {
        public static Type GetTypeFromString(string name)
        {
            return FakeAssembly.GetFakeEntryAssembly().GetType(name);
        }

        public static HashSet<Type> GetTypesFromString(string name, char sep=',')
        {
            HashSet<Type> types = new HashSet<Type>();

            if (string.IsNullOrEmpty(name))
            {
                return types;
            }

            foreach (String s in name.Split(sep).Distinct<string>())
            {
                types.Add(TypeHelper.GetTypeFromString(s));
            }

            return types;
        }

        public static List<Entity> FindTargetEntities(Scene scene, HashSet<Type> targets, bool useTracked)
        {
            return useTracked ? FindTargetEntitiesTracked(scene, targets) : FindTargetEntitiesUntracked(scene, targets);
        }

        public static List<Entity> FindTargetEntitiesTracked(Scene scene, HashSet<Type> targets)
        {
            List<Entity> entities = new List<Entity>();

            foreach (Type type in targets)
            {
                if (type != null && scene.Tracker.Entities.ContainsKey(type))
                {
                    entities.AddRange(scene.Tracker.Entities[type]);
                }
            }

            return entities;
        }

        public static List<Entity> FindTargetEntitiesUntracked(Scene scene, HashSet<Type> targets)
        {
            List<Entity> entities = scene.Entities.Where(entity => targets.Contains(entity.GetType())).ToList();

            return entities;
        }
    }
}