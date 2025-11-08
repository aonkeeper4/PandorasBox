using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.Cil;
using MonoMod;
using Mono.Cecil.Cil;

namespace Celeste.Mod.PandorasBox
{
    [TrackedAs(typeof(Water))]
    [CustomEntity("pandorasBox/coloredWater")]
    class ColoredWater : Water
    {
        private Color baseColor;
        private Color surfaceColor;
        private Color fillColor;
        private Color rayTopColor;
        private bool visibleOnCamera;

        private bool hasTopSurface;
        private bool hasBottomSurface;
        private bool hasLeftSurface;
        private bool hasRightSurface;

        private Vector2 previousPosition;
        private bool trackedPosition = false;
        private bool hasUpdatedFill = false;

        public Surface LeftSurface;
        public Surface RightSurface;

        private bool hasTopRays;
        private bool hasBottomRays;
        private bool hasLeftRays;
        private bool hasRightRays;

        public bool CanJumpOnSurface;

        private static int horizontalVisiblityBuffer = 40;
        private static int verticalVisiblityBuffer = 40;
        private static int horizontalWaterHeightVisiblityBuffer = 24;
        private static int verticalWaterHeightVisiblityBuffer = 24;
        private static int rayMaxLength = 128;

        public static Color CurrentRayTopColor = Color.LightSkyBlue * 0.6f;
        public static bool CurrentlyUpdating;

        private static float cameraTop;
        private static float cameraBottom;
        private static float cameraLeft;
        private static float cameraRight;

        private Rectangle waterFill;
        private HashSet<WaterInteraction> interactionContains;

        public static FieldInfo fillColorField = typeof(Water).GetField("FillColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo surfaceColorField = typeof(Water).GetField("SurfaceColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo rayTopColorField = typeof(Water).GetField("RayTopColor", BindingFlags.Static | BindingFlags.Public);
        public static FieldInfo fillField = typeof(Water).GetField("fill", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo containsField = typeof(Water).GetField("contains", BindingFlags.Instance | BindingFlags.NonPublic);

        public static FieldInfo surfaceMeshField = typeof(Surface).GetField("mesh", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo surfaceFillIndexField = typeof(Surface).GetField("fillStartIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo surfaceSurfaceIndexField = typeof(Surface).GetField("surfaceStartIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo surfaceRayIndexField = typeof(Surface).GetField("rayStartIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo surfaceTimerField = typeof(Surface).GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic);

        public ColoredWater(EntityData data, Vector2 offset) : base(data.Position + offset, data.Bool("hasTop", true), data.Bool("hasBottom"), data.Width, data.Height)
        {
            baseColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));
            surfaceColor = baseColor * 0.8f;
            fillColor = baseColor * 0.3f;
            rayTopColor = baseColor * 0.6f;

            hasLeftSurface = data.Bool("hasLeft");
            hasRightSurface = data.Bool("hasRight");

            hasTopRays = data.Bool("hasTopRays", true);
            hasBottomRays = data.Bool("hasBottomRays", true);
            hasLeftRays = data.Bool("hasLeftRays", true);
            hasRightRays = data.Bool("hasRightRays", true);

            CanJumpOnSurface = data.Bool("canJumpOnSurface", true);

            waterFill = (Rectangle) fillField.GetValue(this);
            interactionContains = (HashSet<WaterInteraction>) containsField.GetValue(this);
        }

        private static bool isTrackedSurface(Surface surface)
        {
            return ColoredWater.CurrentlyUpdating;
        }

        private void initializeSurfaces()
        {
            Color origFill = Water.FillColor;
            Color origSurface = Water.SurfaceColor;

            changeColor(fillColorField, origFill, fillColor);
            changeColor(surfaceColorField, origSurface, surfaceColor);

            hasTopSurface = Surfaces.Contains(TopSurface);
            hasBottomSurface = Surfaces.Contains(BottomSurface);

            Surfaces.Clear();

            if (hasTopSurface)
            {
                TopSurface = new Surface(Position + new Vector2(Width / 2f, 8f), new Vector2(0.0f, -1f), Width, Height);
                Surfaces.Add(TopSurface);

                if (!hasTopRays)
                {
                    TopSurface.Rays.Clear();
                }
            }

            if (hasBottomSurface)
            {
                BottomSurface = new Surface(Position + new Vector2(Width / 2f, Height - 8f), new Vector2(0.0f, 1f), Width, Height);
                Surfaces.Add(BottomSurface);

                if (!hasBottomRays)
                {
                    BottomSurface.Rays.Clear();
                }
            }

            if (hasLeftSurface)
            {
                LeftSurface = new Surface(Position + new Vector2(8, Height / 2), new Vector2(-1f, 0f), Height, Width);
                Surfaces.Add(LeftSurface);

                if (!hasLeftRays)
                {
                    LeftSurface.Rays.Clear();
                }
            }

            if (hasRightSurface)
            {
                RightSurface = new Surface(Position + new Vector2(Width - 8, Height / 2), new Vector2(1f, 0f), Height, Width);
                Surfaces.Add(RightSurface);

                if (!hasRightRays)
                {
                    RightSurface.Rays.Clear();
                }
            }

            // Update fill rectangle
            if (!hasUpdatedFill && (hasLeftSurface || hasRightSurface))
            {
                Rectangle fill = (Rectangle) fillField.GetValue(this);
                int newX = fill.X;
                int newWidth = fill.Width;

                if (hasLeftSurface)
                {
                    newX += 8;
                    newWidth -= 8;
                }

                if (hasRightSurface)
                {
                    newWidth -= 8;
                }

                Rectangle newFill = new Rectangle(newX, fill.Y, newWidth, fill.Height);

                fillField.SetValue(this, newFill);

                hasUpdatedFill = true;
            } 

            changeColor(fillColorField, fillColor, origFill);
            changeColor(surfaceColorField, surfaceColor, origSurface);
        }

        private void updateVisiblity()
        {
            bool horizontalCheck = X < cameraRight + horizontalVisiblityBuffer && X + Width > cameraLeft - horizontalVisiblityBuffer;
            bool verticalCheck = Y < cameraBottom + verticalVisiblityBuffer && Y + Height > cameraTop - verticalVisiblityBuffer;

            visibleOnCamera = horizontalCheck && verticalCheck;
        }

        private void changeColor(FieldInfo fieldInfo, Color from, Color to)
        {
            if (from != to)
            {
                fieldInfo.SetValue(null, to);
            }
        }

        public override void Render()
        {
            if (visibleOnCamera)
            {
                Color origFill = Water.FillColor;
                Color origSurface = Water.SurfaceColor;

                changeColor(fillColorField, origFill, fillColor);
                changeColor(surfaceColorField, origSurface, surfaceColor);

                base.Render();

                changeColor(fillColorField, fillColor, origFill);
                changeColor(surfaceColorField, surfaceColor, origSurface);
            }
        }

        private void rippleLeftRightSurfaces()
        {
            if (!visibleOnCamera)
            {
                return;
            }

            foreach (WaterInteraction component in Scene.Tracker.GetComponents<WaterInteraction>())
            {
                Rectangle bounds = component.Bounds;
                bool contains = interactionContains.Contains(component);
                bool colliding = CollideRect(bounds);
                if (contains != colliding)
                {
                    if (LeftSurface != null && bounds.Center.X <= base.Center.X)
                    {
                        LeftSurface.DoRipple(bounds.Center.ToVector2(), 1f);
                    }

                    if (RightSurface != null && bounds.Center.X > base.Center.X)
                    {
                        RightSurface.DoRipple(bounds.Center.ToVector2(), 1f);
                    }
                }

                // Do not add to the contains hashset, leave that to the base Update method
            }
        }

        private void updateSurfacePositionsAndSize()
        {
            if (!trackedPosition)
            {
                previousPosition = Position;
                trackedPosition = true;
            }

            if (previousPosition != Position)
            {
                Vector2 flooredPosition = new Vector2((float)Math.Floor(Position.X), (float)Math.Floor(Position.Y));

                if (hasTopSurface)
                {
                    TopSurface.Position = flooredPosition + new Vector2(Width / 2f, 8f);
                }

                if (hasBottomSurface)
                {
                    BottomSurface.Position = flooredPosition + new Vector2(Width / 2f, Height - 8f);
                }

                if (hasLeftSurface)
                {
                    LeftSurface.Position = flooredPosition + new Vector2(8, Height / 2);
                }

                if (hasRightSurface)
                {
                    RightSurface.Position = flooredPosition + new Vector2(Width - 8, Height / 2);
                }

                previousPosition = Position;
            }
        }

        private void updateCamera(Camera camera)
        {
            cameraTop = camera.Top;
            cameraBottom = camera.Bottom;
            cameraLeft = camera.Left;
            cameraRight = camera.Right;
        }

        public override void Update()
        {

            Level level = Scene as Level;
            Color origRayTop = Water.RayTopColor;

            updateCamera(level.Camera);
            updateVisiblity();

            CurrentlyUpdating = true;
            CurrentRayTopColor = rayTopColor;

            rippleLeftRightSurfaces();

            base.Update();
            updateSurfacePositionsAndSize();

            CurrentRayTopColor = origRayTop;
            CurrentlyUpdating = false;
        }

        public override void Added(Scene scene)
        {
            initializeSurfaces();

            base.Added(scene);
        }

        private static Vector2 getVisibleSurfaceRange(Surface surface)
        {
            int start = 0;
            int stop = surface.Width;

            float x = surface.Position.X;
            float y = surface.Position.Y;
            float halfWidth = surface.Width / 2;

            // In order: Top, Bottom, Left, Right
            if (surface.Outwards.Y == -1)
            {
                x -= halfWidth;
                start = (int)Calc.Clamp(cameraLeft - horizontalWaterHeightVisiblityBuffer - x, 0, surface.Width);
                stop = (int)Calc.Clamp(cameraRight + horizontalWaterHeightVisiblityBuffer - x, 0, surface.Width);
            }
            else if (surface.Outwards.Y == 1)
            {
                x -= halfWidth;
                stop = surface.Width - (int)Calc.Clamp(cameraLeft - horizontalWaterHeightVisiblityBuffer - x, 0, surface.Width);
                start = surface.Width - (int)Calc.Clamp(cameraRight + horizontalWaterHeightVisiblityBuffer - x, 0, surface.Width);
            }
            else if (surface.Outwards.X == -1)
            {
                y -= halfWidth;
                stop = surface.Width - (int)Calc.Clamp(cameraTop - verticalWaterHeightVisiblityBuffer - y, 0, surface.Width);
                start = surface.Width - (int)Calc.Clamp(cameraBottom + verticalWaterHeightVisiblityBuffer - y, 0, surface.Width);
            }
            else if (surface.Outwards.X == 1)
            {
                y -= halfWidth;
                start = (int)Calc.Clamp(cameraTop - verticalWaterHeightVisiblityBuffer - y, 0, surface.Width);
                stop = (int)Calc.Clamp(cameraBottom + verticalWaterHeightVisiblityBuffer - y, 0, surface.Width);
            }

            bool rangeVisible = isSurfaceSectionVisible(surface, start, stop, Surface.BaseHeight);

            if (!rangeVisible)
            {
                // Any range where stop < start is good here
                start = 0;
                stop = -1;
            }

            return new Vector2(start, stop);
        }

        private static bool isSurfaceSectionVisible(Surface surface, float position1, float position2, float depth = Surface.BaseHeight)
        {
            float x1 = surface.Position.X;
            float y1 = surface.Position.Y;
            float x2 = surface.Position.X;
            float y2 = surface.Position.Y;

            float width = surface.Width;
            float halfWidth = width / 2;

            // In order: Top, Bottom, Left, Right
            if (surface.Outwards.Y == -1)
            {
                x1 += position1 - halfWidth;
                x2 += position2 - halfWidth;
                y2 += depth;
            }
            else if (surface.Outwards.Y == 1)
            {
                x1 += halfWidth - position2;
                x2 += halfWidth - position1;
                y1 -= depth;
            }
            else if (surface.Outwards.X == -1)
            {
                y1 += halfWidth - position2;
                y2 += halfWidth - position1;
                x2 += depth;
            }
            else if (surface.Outwards.X == 1)
            {
                y1 += position1 - halfWidth;
                y2 += position2 - halfWidth;
                x1 -= depth;
            }

            bool horizontalCheck = x1 < cameraRight + horizontalWaterHeightVisiblityBuffer && x2 > cameraLeft - horizontalWaterHeightVisiblityBuffer;
            bool verticalCheck = y1 < cameraBottom + verticalWaterHeightVisiblityBuffer && y2 > cameraTop - verticalWaterHeightVisiblityBuffer;

            return horizontalCheck && verticalCheck;
        }

        private static float getCachedSurfaceHeight(Surface surface, float[] cache, float position, float timer)
        {
            int index = (int)Math.Floor(position / Surface.Resolution);

            if (index < 0 || index >= cache.Length)
            {
                return Surface.BaseHeight;
            }

            if (cache[index] == 0f)
            {
                cache[index] = customGetHeight(surface, position, timer);
            }

            return cache[index];
        }

        public static float customGetHeight(Surface surface, float position, float timer)
        {
            if (position < 0f || position > surface.Width)
            {
                return 0f;
            }
            float num = 0f;
            foreach (Ripple ripple in surface.Ripples)
            {
                float distance = Math.Abs(ripple.Position - position);
                float heightMultiplier = 0f;

                if (distance < 12)
                {
                    heightMultiplier = (distance / 16f) * -1.75f + 1f;
                }
                else if (distance < 16)
                {
                    heightMultiplier = -0.75f;
                }
                else if (distance <= 32)
                {
                    heightMultiplier = (distance - 32f) / 16f * 0.75f;
                }
                else
                {
                    heightMultiplier = 0f;
                }

                num += heightMultiplier * ripple.Height * Ease.CubeIn(1f - ripple.Percent);
            }
            num = Calc.Clamp(num, -4f, 4f);
            foreach (Tension tension in surface.Tensions)
            {
                float t = Calc.ClampedMap(Math.Abs(tension.Position - position), 0f, 24f, 1f, 0f);
                num += Ease.CubeOut(t) * tension.Strength * 12f;
            }
            float val = position / surface.Width;
            num *= Math.Min(0.5f + val * 5f, 1f);
            num *= Math.Min(0.5f + (1f - val) * 5f, 1f);
            num += (float)Math.Sin(timer + position * 0.1f);
            return num + 6f;
        }

        private static void Surface_Update(On.Celeste.Water.Surface.orig_Update orig, Surface self)
        {
            if (!isTrackedSurface(self))
            {
                orig(self);

                return;
            }

            float timer = (float) surfaceTimerField.GetValue(self);
            surfaceTimerField.SetValue(self, timer + Engine.DeltaTime);

            Vector2 perpendicular = self.Outwards.Perpendicular();
            for (int num = self.Ripples.Count - 1; num >= 0; num--)
            {
                Ripple ripple = self.Ripples[num];
                if (ripple.Percent > 1f)
                {
                    self.Ripples.RemoveAt(num);
                }
                else
                {
                    ripple.Position += ripple.Speed * Engine.DeltaTime;
                    if (ripple.Position < 0f || ripple.Position > self.Width)
                    {
                        ripple.Speed = 0f - ripple.Speed;
                        ripple.Position = Calc.Clamp(ripple.Position, 0f, self.Width);
                    }
                    ripple.Percent += Engine.DeltaTime / ripple.Duration;
                }
            }

            bool surfaceVisible = isSurfaceSectionVisible(self, 0, self.Width, rayMaxLength);

            if (!surfaceVisible)
            {
                return;
            }

            VertexPositionColor[] mesh = (VertexPositionColor[])surfaceMeshField.GetValue(self);
            int fillStartIndex = (int)surfaceFillIndexField.GetValue(self);
            int surfaceStartIndex = (int)surfaceSurfaceIndexField.GetValue(self);
            int rayStartIndex = (int)surfaceRayIndexField.GetValue(self);

            float[] surfaceHeights = new float[(int)Math.Ceiling((float)self.Width / Surface.Resolution) + 1];

            Vector2 visibilityRange = getVisibleSurfaceRange(self);
            int visibleSurfaceStart = (int)visibilityRange.X;
            int visibleSurfaceEnd = (int)visibilityRange.Y;
            int position = visibleSurfaceStart;

            int num3 = fillStartIndex;
            int num4 = surfaceStartIndex;

            float halfWidth = self.Width / 2;
            float surfaceHeight = getCachedSurfaceHeight(self, surfaceHeights, position, timer);

            while (position < visibleSurfaceEnd)
            {
                int num5 = position;
                int num6 = Math.Min(position + Surface.Resolution, self.Width);
                float surfaceHeightNext = getCachedSurfaceHeight(self, surfaceHeights, num6, timer);

                Vector2 perpendicularHeight = self.Outwards * surfaceHeight;
                Vector2 perpendicularHeightNext = self.Outwards * surfaceHeightNext;
                Vector2 positionCalcCurrent = self.Position + perpendicular * (-halfWidth + num5);
                Vector2 positionCalcNext = self.Position + perpendicular * (-halfWidth + num6);

                mesh[num3].Position = new Vector3(positionCalcCurrent + perpendicularHeight, 0f);
                mesh[num3 + 1].Position = new Vector3(positionCalcNext + perpendicularHeightNext, 0f);
                mesh[num3 + 2].Position = new Vector3(positionCalcCurrent, 0f);
                mesh[num3 + 3].Position = new Vector3(positionCalcNext + perpendicularHeightNext, 0f);
                mesh[num3 + 4].Position = new Vector3(positionCalcNext, 0f);
                mesh[num3 + 5].Position = new Vector3(positionCalcCurrent, 0f);
                mesh[num4].Position = new Vector3(positionCalcCurrent + self.Outwards * (surfaceHeight + 1f), 0f);
                mesh[num4 + 1].Position = new Vector3(positionCalcNext + self.Outwards * (surfaceHeightNext + 1f), 0f);
                mesh[num4 + 2].Position = new Vector3(positionCalcCurrent + perpendicularHeight, 0f);
                mesh[num4 + 3].Position = new Vector3(positionCalcNext + self.Outwards * (surfaceHeightNext + 1f), 0f);
                mesh[num4 + 4].Position = new Vector3(positionCalcNext + perpendicularHeightNext, 0f);
                mesh[num4 + 5].Position = new Vector3(positionCalcCurrent + perpendicularHeight, 0f);
                position += Surface.Resolution;
                num3 += 6;
                num4 += 6;
                surfaceHeight = surfaceHeightNext;
            }
            Vector2 value2 = self.Position + perpendicular * ((float)(-self.Width) / 2f);
            int num7 = rayStartIndex;
            bool surfaceSegmentVisible = isSurfaceSectionVisible(self, 0, self.Width, rayMaxLength);

            // Skip any rendering related updates, progressing the Percent and Resetting is needed
            foreach (Ray ray in self.Rays)
            {
                if (ray.Percent > 1f)
                {
                    ray.Reset(0f);
                }
                ray.Percent += Engine.DeltaTime / ray.Duration;
                float scale = 1f;
                
                if (!surfaceSegmentVisible)
                {
                    num7 += 6;

                    continue;
                }

                float num8 = Math.Max(0f, ray.Position - ray.Width / 2f);
                float num9 = Math.Min(self.Width, ray.Position + ray.Width / 2f);
                bool sectionVisible = isSurfaceSectionVisible(self, num8, num9, rayMaxLength);

                if (!sectionVisible)
                {
                    num7 += 6;

                    continue;
                }

                if (ray.Percent < 0.1f)
                {
                    scale = ray.Percent * 10f;
                }
                else if (ray.Percent > 0.9f)
                {
                    scale = 1f - (ray.Percent - 0.9f) * 10f;
                }
                float scaleFactor = Math.Min(self.BodyHeight, 0.7f * ray.Length);
                Vector2 scaledOutwards = self.Outwards * scaleFactor;
                Color color = ColoredWater.CurrentRayTopColor * scale;
                float num10 = 0.3f * ray.Length;
                Vector2 value3 = value2 + perpendicular * num8 + self.Outwards * getCachedSurfaceHeight(self, surfaceHeights, num8, timer);
                Vector2 value4 = value2 + perpendicular * num9 + self.Outwards * getCachedSurfaceHeight(self, surfaceHeights, num9, timer);
                Vector2 value5 = value2 + perpendicular * (num9 - num10) - scaledOutwards;
                Vector2 value6 = value2 + perpendicular * (num8 - num10) - scaledOutwards;
                Vector3 value7 = new Vector3(value4, 0f);
                Vector3 value8 = new Vector3(value6, 0f);
                mesh[num7].Position = new Vector3(value3, 0f);
                mesh[num7].Color = color;
                mesh[num7 + 1].Position = value7;
                mesh[num7 + 1].Color = color;
                mesh[num7 + 2].Position = value8;
                mesh[num7 + 3].Position = value7;
                mesh[num7 + 3].Color = color;
                mesh[num7 + 4].Position = new Vector3(value5, 0f);
                mesh[num7 + 5].Position = value8;
                num7 += 6;
            }
        }

        private static void Player_NormalUpdate(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt<Surface>("DoRipple")))
            {
                Logger.Log($"{PandorasBoxModule.LoggerTag}/PlayerNormalUpdate", $"Patching water surface jumping at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                ILLabel skipJump = cursor.DefineLabel();
                ILLabel keepJump = cursor.DefineLabel();

                cursor.GotoPrev(MoveType.After, instr => instr.MatchCallOrCallvirt<Player>("Jump"));
                cursor.Emit(OpCodes.Br, keepJump);
                cursor.MarkLabel(skipJump);
                cursor.EmitDelegate<Action>(() =>
                {
                    // TODO: Include something here? This is when the player jumps on the surface
                });
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Pop);
                cursor.MarkLabel(keepJump);

                cursor.GotoPrev(MoveType.Before, instr => instr.MatchCallOrCallvirt<Player>("Jump"));
                cursor.Emit(OpCodes.Ldloc_S, (byte)13);
                cursor.EmitDelegate<Func<Water, bool>>((water) =>
                {
                    // Returns wheter we allow the jump or not
                    ColoredWater coloredWater = (water as ColoredWater);

                    if (coloredWater != null)
                    {
                        return coloredWater.CanJumpOnSurface;
                    }
                    
                    return true;
                });
                cursor.Emit(OpCodes.Brfalse, skipJump);

                cursor.GotoNext(MoveType.Before, instr => instr.MatchCallOrCallvirt<Surface>("DoRipple"));
            } 
        }

        public static void Load()
        {
            On.Celeste.Water.Surface.Update += Surface_Update;

            IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
        }

        public static void Unload()
        {
            On.Celeste.Water.Surface.Update -= Surface_Update;

            IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
        }
    }
}
