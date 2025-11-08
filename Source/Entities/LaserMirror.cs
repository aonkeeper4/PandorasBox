using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/laserMirror")]
    class LaserMirror : LaserDetectorActor
    {
        private Sprite sprite;
        private Dictionary<Laserbeam, Laserbeam> reflections;
        private HashSet<Laserbeam> createdLasers;
        private string opening;

        private static Dictionary<string, Tuple<int, int>> openingScales = new Dictionary<string, Tuple<int, int>>()
        {
            {"LeftUp", new Tuple<int, int>(1, 1)},
            {"UpRight", new Tuple<int, int>(-1, 1)},
            {"RightDown", new Tuple<int, int>(-1, -1)},
            {"DownLeft", new Tuple<int, int>(1, -1)},
        };

        private static Dictionary<string, Vector2> reflectionOffsets = new Dictionary<string, Vector2>()
        {
            {"Right", new Vector2(-4, 0)},
            {"Down", new Vector2(0, -4)},
            {"Left", new Vector2(4, 0)},
            {"Up", new Vector2(0, 4)},
        };

        private Dictionary<string, Collider> openingBlockingColliders = new Dictionary<string, Collider>()
        {
            {
                "LeftUp",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, 5f),
                    new Hitbox(3f, 16f, 5f, -8f)
                })
            },
            {
                "UpRight",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, 5f),
                    new Hitbox(3f, 16f, -8f, -8f)
                })
            },
            {
                "RightDown",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, -8f),
                    new Hitbox(3f, 16f, -8f, -8f)
                })
            },
            {
                "DownLeft",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, -8f),
                    new Hitbox(3f, 16f, 5f, -8f)
                })
            },
        };

        public LaserMirror(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            opening = data.Attr("opening", "LeftUp");
            Tuple<int, int> scales = openingScales[opening];
            int scaleX = scales.Item1;
            int scaleY = scales.Item2;

            Add((Component)(sprite = new Sprite(GFX.Game, "objects/pandorasBox/laser/mirror/mirror_static")));
            sprite.AddLoop("mirror_static", "", 0.125f);
            sprite.CenterOrigin();
            sprite.Scale = new Vector2(scaleX, scaleY);

            sprite.Play("mirror_static");

            reflections = new Dictionary<Laserbeam, Laserbeam>();
            createdLasers = new HashSet<Laserbeam>();

            Collider = openingBlockingColliders[opening];
            LaserBlockingCollider = openingBlockingColliders[opening];
            LaserDetectionCollider = new Hitbox(2f, 2f, -1f, -1f);

            Depth = 50;
            
            Add(new StaticMover
            {
                SolidChecker = new Func<Solid, bool>(IsRiding),
                JumpThruChecker = new Func<JumpThru, bool>(IsRiding),
                OnMove = delegate (Vector2 v)
                {
                    // Prevent default behavior, we handle it manually
                },
                OnDestroy = delegate ()
                {
                    foreach (var beam in createdLasers)
                    {
                        beam.RemoveSelf();
                    }

                    RemoveSelf();
                }
            });

            DetectionDelay = 1;
        }

        private void reflect(Laserbeam beam)
        {
            if (!reflections.ContainsKey(beam))
            {
                Level level = Scene as Level;
                string newDirection = LaserHelper.GetReflection(opening, beam);

                if (newDirection != null)
                {
                    Laserbeam reflectionBeam = new Laserbeam(Position, newDirection, beam.Color, beam.TTL, this);
                    reflections[beam] = reflectionBeam;
                    level.Add(reflectionBeam);
                    createdLasers.Add(reflectionBeam);
                    reflections[beam].Depth = beam.Depth + 1;
                    reflections[beam].Position += reflectionOffsets[newDirection];
                }
            }
        }

        public override void OnLaserbeams(List<Laserbeam> laserbeams)
        {
            HashSet<Laserbeam> needsUpdate = new HashSet<Laserbeam>(reflections.Keys);

            foreach (Laserbeam beam in laserbeams)
            {
                reflect(beam);
                needsUpdate.Remove(beam);
            }

            foreach (Laserbeam beam in needsUpdate)
            {
                Laserbeam target = reflections[beam];

                target.RemoveSelf();
                reflections.Remove(beam);
                createdLasers.Remove(beam);
            }
        }
    }
}
