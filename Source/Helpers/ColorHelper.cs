using Microsoft.Xna.Framework;
using System;
using Monocle;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    class ColorHelper
    {
        private static readonly System.Reflection.PropertyInfo[] colorProps = typeof(Color).GetProperties();

        public static Color GetColor(String color)
        {
            foreach (var c in colorProps)
            {
                if (color.Equals(c.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return (Color)c.GetValue(new Color(), null);
                }
            }

            try
            {
                return Calc.HexToColor(color.Replace("#", ""));
            }
            catch
            {

            }

            return Color.Transparent;
        }

        public static List<Color> GetColors(String rawColors, Char sep=',')
        {
            List<Color> colors = new List<Color>();

            foreach (String s in rawColors.Split(sep))
            {
                colors.Add(ColorHelper.GetColor(s));
            }

            return colors;
        }
    }
}
