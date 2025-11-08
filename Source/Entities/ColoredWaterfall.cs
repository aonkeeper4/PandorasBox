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
    [CustomEntity("pandorasBox/coloredWaterfall")]
    class ColoredWaterfall : Actor
    {
        private static int horizontalVisiblityBuffer = 32;
        private static int verticalVisiblityBuffer = 32;

        private float height;
        private Water water;
        private Color baseColor;
        private Color surfaceColor;
        private Color fillColor;
        private Color rayTopColor;
        private Solid solid;
        private SoundSource loopingSfx;
        private SoundSource enteringSfx;
        private bool visibleOnCamera;

        public ColoredWaterfall(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            baseColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));
            surfaceColor = baseColor * 0.8f;
            fillColor = baseColor * 0.3f;
            rayTopColor = baseColor * 0.6f;
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            Depth = -9999;
            Tag = Tags.TransitionUpdate;

            Level level = Scene as Level;
            bool deep = water != null && !Scene.CollideCheck<Solid>(new Rectangle((int)X, (int)(Y + height), 8, 16));

            for (height = 8f; Y + height < level.Bounds.Bottom && (water = Scene.CollideFirst<Water>(new Rectangle((int)X, (int)(Y + height), 8, 8))) == null && ((solid = Scene.CollideFirst<Solid>(new Rectangle((int)X, (int)(Y + height), 8, 8))) == null || !solid.BlockWaterfalls); solid = null)
            {
                height += 8f;
            }

            Add((loopingSfx = new SoundSource()));
            loopingSfx.Play("event:/env/local/waterfall_small_main");

            Add((enteringSfx = new SoundSource()));
            enteringSfx.Play(deep ? "event:/env/local/waterfall_small_in_deep" : "event:/env/local/waterfall_small_in_shallow");
            enteringSfx.Position.Y = height;

            Add(new DisplacementRenderHook(new Action(RenderDisplacement)));
        }

        public void RenderDisplacement()
        {
            Draw.Rect(X, Y, 8f, height, new Color(0.5f, 0.5f, 0.8f, 1f));
        }

        private void updateVisiblity(Level level)
        {
            Camera camera = level.Camera;

            bool horizontalCheck = X < camera.Right + horizontalVisiblityBuffer && X > camera.Left - horizontalVisiblityBuffer;
            bool verticalCheck = Y < camera.Bottom + verticalVisiblityBuffer && Y + height > camera.Top - verticalVisiblityBuffer;

            visibleOnCamera = horizontalCheck && verticalCheck;
        }

        public override void Update()
        {
            Level level = Scene as Level;

            loopingSfx.Position.Y = Calc.Clamp(level.Camera.Position.Y + 90f, Y, height);

            if (Scene.OnInterval(0.05f))
            {
                updateVisiblity(level);
            }

            // Don't ripple if the water is inactive
            // This can cause it to build up a lot of ripples when paired with the entity activator
            if (water != null && water.Active && water.TopSurface != null && Scene.OnInterval(0.3f))
            {
                water.TopSurface.DoRipple(new Vector2(X + 4f, water.Y), 0.75f);
            }

            if (visibleOnCamera)
            {
                if (water != null || solid != null)
                {
                    Vector2 position = new Vector2(X + 4f, (float)(Y + height + 2.0));
                    level.ParticlesFG.Emit(Water.P_Splash, 1, position, new Vector2(8f, 2f), baseColor, new Vector2(0.0f, -1f).Angle());
                }
            }

            base.Update();
        }

        public override void Render()
        {
            if (visibleOnCamera)
            {
                if (water == null || water.TopSurface == null)
                {
                    Draw.Rect(X + 1f, Y, 6f, height, fillColor);
                    Draw.Rect(X - 1f, Y, 2f, height, surfaceColor);
                    Draw.Rect(X + 7f, Y, 2f, height, surfaceColor);
                }
                else
                {
                    Water.Surface topSurface = water.TopSurface;
                    float num = height + water.TopSurface.Position.Y - water.Y;

                    for (int index = 0; index < 6; ++index)
                    {
                        Draw.Rect((float)(X + index + 1f), Y, 1f, num - topSurface.GetSurfaceHeight(new Vector2(X + 1f + index, water.Y)), fillColor);
                    }

                    Draw.Rect(X - 1f, Y, 2f, num - topSurface.GetSurfaceHeight(new Vector2(X, water.Y)), surfaceColor);
                    Draw.Rect(X + 7f, Y, 2f, num - topSurface.GetSurfaceHeight(new Vector2(X + 8f, water.Y)), surfaceColor);
                }
            }
        }
    }
}
