using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/coloredBigWaterfall")]
    class ColoredBigWaterfall : BigWaterfall
    {
        private Color baseColor;

        public static FieldInfo fillColorField = typeof(BigWaterfall).GetField("fillColor", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo surfaceColorField = typeof(BigWaterfall).GetField("surfaceColor", BindingFlags.Instance | BindingFlags.NonPublic);

        private bool fixedColors;

        public ColoredBigWaterfall(EntityData data, Vector2 offset) : base(data, offset)
        {
            baseColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));
        }

        public override void Awake(Scene scene)
        {
            Color surfaceColor = baseColor * 0.8f;
            Color fillColor = baseColor * 0.3f;

            surfaceColorField.SetValue(this, surfaceColor);
            fillColorField.SetValue(this, fillColor);
        }
    }
}
