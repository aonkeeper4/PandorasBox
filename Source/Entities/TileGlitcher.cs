using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/tileGlitcher")]
    class TileGlitcher : Entity
    {
        private static FieldInfo lookupFieldInfo = typeof(Autotiler).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo terrainTypeInfo = typeof(Autotiler).GetField("TerrainType", BindingFlags.Instance | BindingFlags.NonPublic);

        private static List<char> validFgTiles;
        private static List<char> validBgTiles;

        private static int frameGroups = 4;

        private bool allowAir;
        private bool transformAir;

        private string target;
        private float threshold;
        private float rate;

        private float width;
        private float height;

        private string customFgTiles;
        private string customBgTiles;

        private bool active;
        private bool glitcherAdded;

        private int frameGroup;

        private string flag;

        public TileGlitcher(EntityData data, Vector2 offset) : this(data.Position + offset, data.Width, data.Height, data.Bool("allowAir", false), data.Bool("transformAir", false), data.Attr("target", "Both"), data.Float("threshold", 0.1f), data.Float("rate", 0.05f), data.Attr("flag", ""), data.Attr("customFgTiles", ""), data.Attr("customBgTiles", ""))
        {

        }

        public TileGlitcher(Vector2 position, float width, float height, bool allowAir, bool transformAir, string target, float threshold, float rate, string flag, string customFgTiles, string customBgTiles) : base(position)
        {
            this.width = width;
            this.height = height;
            this.allowAir = allowAir;
            this.transformAir = transformAir;
            this.target = target;
            this.threshold = threshold;
            this.rate = rate;
            this.flag = flag;
            this.customFgTiles = customFgTiles;
            this.customBgTiles = customBgTiles;

            frameGroup = Calc.Random.Next(frameGroups);

            active = true;

            cacheValidTiles();
        }

        private static void cacheValidTiles()
        {
            if (validFgTiles == null || validBgTiles == null)
            {
                var fgValidTilesDict = lookupFieldInfo.GetValue(GFX.FGAutotiler) as IDictionary;
                var bgValidTilesDict = lookupFieldInfo.GetValue(GFX.BGAutotiler) as IDictionary;

                validFgTiles = fgValidTilesDict.Keys.Cast<char>().ToList();
                validBgTiles = bgValidTilesDict.Keys.Cast<char>().ToList();
            }
        }

        private static void clearValidTiles()
        {
            validFgTiles = null;
            validBgTiles = null;
        }

        public override void Update()
        {
            Level level = Scene as Level;

            int currentFrameGroup = (int)(Engine.FrameCounter % (ulong)frameGroups);

            if (!string.IsNullOrEmpty(flag))
            {
                active = flag.Split(new char[] {','}).Any((string f) => level.Session.GetFlag(f));
            }

            if (active && !glitcherAdded && currentFrameGroup == frameGroup)
            {
                Add((Component)new Coroutine(tileGlitcher(), true));
            }

            base.Update();
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            clearValidTiles();
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);

            clearValidTiles();
        }

        private IEnumerator tileGlitcher()
        {
            glitcherAdded = true;

            Level level = Scene as Level;

            Vector2 pos = (Position - level.LevelOffset) / 8f;

            int ox = (int)Math.Round(pos.X + level.LevelSolidOffset.X);
            int oy = (int)Math.Round(pos.Y + level.LevelSolidOffset.Y);

            int tw = (int)Math.Ceiling(width / 8f);
            int th = (int)Math.Ceiling(height / 8f);

            bool glitchFg = target.Equals("FG") || target.Equals("Both");
            bool glitchBg = target.Equals("BG") || target.Equals("Both");

            cacheValidTiles();

            List<Char> validFg = new List<char>(validFgTiles);
            List<Char> validBg = new List<char>(validBgTiles);

            if (allowAir)
            {
                validFg.Add('0');
                validBg.Add('0');
            }

            validFg = string.IsNullOrEmpty(customFgTiles) ? validFg : customFgTiles.Where(c => validFg.Contains(c)).ToList();
            validBg = string.IsNullOrEmpty(customBgTiles) ? validBg : customBgTiles.Where(c => validBg.Contains(c)).ToList();

            while (active)
            {
                if (glitchFg && validFg.Count > 0)
                {
                    VirtualMap<char> fgData = level.SolidsData;
                    VirtualMap<MTexture> fgTexes = level.SolidTiles.Tiles.Tiles;

                    VirtualMap<bool> collision = ((Grid)level.SolidTiles.Collider).Data;

                    VirtualMap<char> newFgData = new VirtualMap<char>(tw + 2, th + 2, '0');

                    for (int x = ox - 1; x < ox + tw + 1; x++)
                    {
                        for (int y = oy - 1; y < oy + th + 1; y++)
                        {
                            if (x > 0 && x < fgTexes.Columns && y > 0 && y < fgTexes.Rows && (transformAir || fgData[x, y] != '0'))
                            {
                                newFgData[x - ox + 1, y - oy + 1] = fgData[x, y];

                                if (Calc.Random.NextFloat() < threshold)
                                {
                                    char value = validFg[Calc.Random.Next(validFg.Count)];
                                    newFgData[x - ox + 1, y - oy + 1] = value;
                                }
                            }
                        }
                    }

                    Autotiler.Generated newFgTiles = GFX.FGAutotiler.GenerateMap(newFgData, true);

                    for (int x = ox - 1; x < ox + tw + 1; x++)
                    {
                        for (int y = oy - 1; y < oy + th + 1; y++)
                        {
                            if (x > 0 && x < fgTexes.Columns && y > 0 && y < fgTexes.Rows)
                            {
                                if (x >= ox && x < ox + tw && y >= oy && y < oy + th && fgTexes[x, y] != newFgTiles.TileGrid.Tiles[x - ox + 1, y - oy + 1])
                                {
                                    fgData[x, y] = newFgData[x - ox + 1, y - oy + 1];
                                    fgTexes[x, y] = newFgTiles.TileGrid.Tiles[x - ox + 1, y - oy + 1];

                                    bool newCollision = fgTexes[x, y] != null;

                                    if (collision[x, y] != newCollision)
                                    {
                                        collision[x, y] = newCollision;
                                    }
                                }
                            }
                        }
                    }
                }

                if (glitchBg && validBg.Count > 0)
                {
                    VirtualMap<char> bgData = level.BgData;
                    VirtualMap<MTexture> bgTexes = level.BgTiles.Tiles.Tiles;

                    VirtualMap<char> newBgData = new VirtualMap<char>(tw + 2, th + 2, '0');

                    for (int x = ox - 1; x < ox + tw + 1; x++)
                    {
                        for (int y = oy - 1; y < oy + th + 1; y++)
                        {
                            if (x > 0 && x < bgTexes.Columns && y > 0 && y < bgTexes.Rows && (transformAir || bgData[x, y] != '0'))
                            {
                                newBgData[x - ox + 1, y - oy + 1] = bgData[x, y];

                                if (Calc.Random.NextFloat() < threshold)
                                {
                                    char value = validBg[Calc.Random.Next(validBg.Count)];
                                    newBgData[x - ox + 1, y - oy + 1] = value;
                                }
                            }
                        }
                    }

                    Autotiler.Generated newBgTiles = GFX.BGAutotiler.GenerateMap(newBgData, true);

                    for (int x = ox - 1; x < ox + tw + 1; x++)
                    {
                        for (int y = oy - 1; y < oy + th + 1; y++)
                        {
                            if (x > 0 && x < bgTexes.Columns && y > 0 && y < bgTexes.Rows)
                            {
                                if (x >= ox && x < ox + tw && y >= oy && y < oy + th && bgTexes[x, y] != newBgTiles.TileGrid.Tiles[x - ox + 1, y - oy + 1])
                                {
                                    bgData[x, y] = newBgData[x - ox + 1, y - oy + 1];
                                    bgTexes[x, y] = newBgTiles.TileGrid.Tiles[x - ox + 1, y - oy + 1];
                                }
                            }
                        }
                    }
                }

                yield return rate;
            }

            glitcherAdded = false;

            yield return true;
        }
    }
}
