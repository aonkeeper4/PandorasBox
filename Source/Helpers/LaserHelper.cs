using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    class LaserHelper
    {
        public static Dictionary<string, float> DirectionRotations = new Dictionary<string, float>()
        {
            {"Right", 0f},
            {"Down", (float)Math.PI / 2f},
            {"Left", (float)Math.PI},
            {"Up", (float)Math.PI * 3f / 2f},
        };

        public static Dictionary<string, Func<Laserbeam, Collider, float>> LengthCalculatingFunctions = new Dictionary<string, Func<Laserbeam, Collider, float>>()
        {
            {"Right", (beam, collider) => Math.Max(collider.AbsoluteLeft - beam.Position.X, 0)},
            {"Down", (beam, collider) => Math.Max(collider.AbsoluteTop - beam.Position.Y, 0)},
            {"Left", (beam, collider) => Math.Max(beam.Position.X - collider.AbsoluteRight, 0)},
            {"Up", (beam, collider) => Math.Max(beam.Position.Y - collider.AbsoluteBottom, 0)},
        };


        public static Dictionary<String, Func<Level, Laserbeam, float>> MaxLengthFunction = new Dictionary<string, Func<Level, Laserbeam, float>>()
        {
            {"Left", (level, beam) => beam.Position.X - level.Bounds.Left},
            {"Up", (level, beam) => beam.Position.Y - level.Bounds.Top},
            {"Right", (level, beam) => level.Bounds.Right - beam.Position.X},
            {"Down", (level, beam) => level.Bounds.Bottom - beam.Position.Y},
        };


        public static Dictionary<String, Dictionary<String, String>> OpeningRelfections = new Dictionary<String, Dictionary<String, String>>(){
            {"LeftUp", new Dictionary<String, String>()
                {
                    {"Right", "Up"},
                    {"Down", "Left"}
                }
            },
            {"UpRight", new Dictionary<String, String>()
                {
                    {"Down", "Right"},
                    {"Left", "Up"}
                }
            },
            {"RightDown", new Dictionary<String, String>()
                {
                    {"Left", "Down"},
                    {"Up", "Right"}
                }
            },
            {"DownLeft", new Dictionary<String, String>()
                {
                    {"Up", "Left"},
                    {"Right", "Down"}
                }
            },
        };

        public static float MaxBeamLength(Level level, Laserbeam beam)
        {
            return MaxLengthFunction[beam.Direction](level, beam);
        }

        public static string GetReflection(string opening, string direction)
        {
            return OpeningRelfections.TryGetValue(opening, out var fromTo) && fromTo.TryGetValue(direction, out var newDirection) ? newDirection : null;
        }

        public static string GetReflection(string opening, Laserbeam beam)
        {
            return GetReflection(opening, beam.Direction);
        }

        public static List<Laserbeam> ConnectedLasers(Scene scene, LaserDetectorActor entity)
        {
            Collider origCollider = entity.Collider;
            entity.Collider = entity.LaserDetectionCollider;

            List<Laserbeam> res = scene.Tracker.GetEntities<Laserbeam>().FindAll(beam => entity.CollideCheck(beam)).Cast<Laserbeam>().ToList<Laserbeam>();

            entity.Collider = origCollider;

            return res;
        }

        public static bool LaserBlockingCheck(Scene scene, Laserbeam beam)
        {
            var res = scene.Tracker.GetEntities<LaserDetectorActor>().Cast<LaserDetectorActor>().Any(entity => entity.LaserBlockingCheck(beam));

            return res;
        }

        public static float GetLaserLengthDACOnGrid(Laserbeam beam, Grid grid, float checking)
        {
            float origLength = beam.Length;

            float offset = checking / 2f;
            beam.Length = offset;

            while (offset >= 1f)
            {
                bool colliding = grid.Collide(beam);
                int sign = colliding ? -1 : 1;

                offset /= 2f;
                beam.Length += sign * offset;
            }

            float res = beam.Length;
            beam.Length = origLength;

            return res;
        }

        public static void SetLaserLength(Scene scene, Laserbeam beam)
        {
            Level level = scene as Level;

            beam.Length = MaxBeamLength(level, beam);
            float shortestWidth = MaxBeamLength(level, beam);

            foreach (Solid s in scene.Tracker.GetEntities<Solid>())
            {
                if (beam.CollideCheck(s))
                {
                    if (s.Collider is Grid)
                    {
                        shortestWidth = Math.Min(shortestWidth, GetLaserLengthDACOnGrid(beam, s.Collider as Grid, shortestWidth));
                    }
                    else
                    {
                        shortestWidth = Math.Min(shortestWidth, LengthCalculatingFunctions[beam.Direction](beam, s.Collider));
                    }
                }
            }

            foreach (LaserDetectorActor entity in scene.Tracker.GetEntities<LaserDetectorActor>())
            {
                Collider origCollider = entity.Collider;

                Collider collider = entity.GetLaserBlockingCollider(beam);
                ColliderList colliderList = collider as ColliderList;

                if (colliderList == null)
                {
                    entity.Collider = collider;
                    if (beam.CollideCheck(entity))
                    {
                        shortestWidth = Math.Min(shortestWidth, LengthCalculatingFunctions[beam.Direction](beam, collider));
                    }
                }
                else
                {
                    foreach (Collider c in colliderList.colliders)
                    {
                        entity.Collider = c;
                        if (beam.CollideCheck(entity))
                        {
                            shortestWidth = Math.Min(shortestWidth, LengthCalculatingFunctions[beam.Direction](beam, c));
                        }
                    }
                }

                entity.Collider = origCollider;
            }

            beam.Length = shortestWidth;
        }

        public static void SetLaserLengthDAQ(Scene scene, Laserbeam beam)
        {
            Level level = scene as Level;
            float maxLength = LaserHelper.MaxBeamLength(level, beam);

            float offset = maxLength / 2f;
            beam.Length = offset;

            while (offset >= 1f)
            {
                bool colliding = beam.CollideCheck<Solid>() || LaserHelper.LaserBlockingCheck(scene, beam);
                int sign = colliding ? -1 : 1;

                offset /= 2f;
                beam.Length += sign * offset;
            }

            beam.Length = (float)Math.Round(beam.Length);
        }
    }
}
