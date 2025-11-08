using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    class PlayerHelper
    {
        private static MethodInfo playerSwimCheck = typeof(Player).GetMethod("SwimCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo playerSwimUnderwaterCheck = typeof(Player).GetMethod("SwimUnderwaterCheck", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool PlayerInWater(Player player, string mode = "Swimming")
        {
            if (player != null)
            {
                if (mode == "Swimming")
                {
                    return (bool)playerSwimCheck.Invoke(player, new object[] { });
                }
                else if (mode == "Diving")
                {
                    return (bool)playerSwimUnderwaterCheck.Invoke(player, new object[] { });
                }
            }

            return false;
        }
    }
}
