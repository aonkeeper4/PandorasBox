using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    class CircularResortPlatformRail : Entity
    {
        public Color LineEdgeColor;
        public Color LineFillColor;
        public float PlatformWidth;
        public float Length;

        public CircularResortPlatformRail(Vector2 position, Color lineEdgeColor, Color lineFillColor, float width, float length)
        {
            PlatformWidth = width;
            Length = length;

            Position = position;

            LineEdgeColor = lineEdgeColor;
            LineFillColor = lineFillColor;

            Depth = 9001;
        }

        public override void Render()
        {
            Vector2 pos = Position + new Vector2(PlatformWidth / 2f, 4);

            Draw.Circle(pos, Length, LineEdgeColor, 4f, 32);
            Draw.Circle(pos, Length, LineFillColor, 2f, 32);
        }
    }
}
