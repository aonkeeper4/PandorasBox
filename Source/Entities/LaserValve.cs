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
    [CustomEntity("pandorasBox/laserValve")]
    class LaserValve : LaserDetectorActor
    {
        private int delay;
        private string direction;

        private Sprite sprite;
        private List<Laserbeam> knownBeams;

        private Dictionary<string, Collider> direcitonBlockingColliders = new Dictionary<string, Collider>()
        {
            {
                "Right",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, -8f),
                    new Hitbox(16f, 3f, -8f, 5f),
                    new Hitbox(10f, 10f, -5f, -5f),
                })
            },
            {
                "Down",
                new ColliderList(new Collider[] {
                    new Hitbox(3f, 16f, -8f, -8f),
                    new Hitbox(3f, 16f, 5f, -8f),
                    new Hitbox(10f, 10f, -5f, -5f),
                })
            },
            {
                "Left",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, -8f),
                    new Hitbox(16f, 3f, -8f, 5f),
                    new Hitbox(10f, 10f, -5f, -5f),
                })
            },
            {
                "Up",
                new ColliderList(new Collider[] {
                    new Hitbox(3f, 16f, -8f, -8f),
                    new Hitbox(3f, 16f, 5f, -8f),
                    new Hitbox(10f, 10f, -5f, -5f),
                })
            },
        };

        private Dictionary<string, Collider> direcitonKnownBlockingColliders = new Dictionary<string, Collider>()
        {
            {
                "Right",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, -8f),
                    new Hitbox(16f, 3f, -8f, 5f),
                })
            },
            {
                "Down",
                new ColliderList(new Collider[] {
                    new Hitbox(3f, 16f, -8f, -8f),
                    new Hitbox(3f, 16f, 5f, -8f),
                })
            },
            {
                "Left",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, -8f),
                    new Hitbox(16f, 3f, -8f, 5f),
                })
            },
            {
                "Up",
                new ColliderList(new Collider[] {
                    new Hitbox(3f, 16f, -8f, -8f),
                    new Hitbox(3f, 16f, 5f, -8f),
                })
            },
        };

        private Dictionary<string, Collider> directionDetectingColliders = new Dictionary<string, Collider>()
        {
            {
                "Right",
                new Hitbox(3f, 10f, -8f, -5f)
            },
            {
                "Down",
                 new Hitbox(10f, 3f, -5f, -8f)
            },
            {
                "Left",
                 new Hitbox(3f, 10f, 5f, -5f)
            },
            {
                "Up",
                 new Hitbox(10f, 3f, -5f, 5f)
            },
        };

        private Dictionary<string, Vector2> repeatOffsets = new Dictionary<string, Vector2>()
        {
            {"Right", new Vector2(8f, 0f)},
            {"Down", new Vector2(0f, 8f)},
            {"Left", new Vector2(-8f, 0f)},
            {"Up", new Vector2(0f, -8f)}
        };

        public LaserValve(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            delay = data.Int("delay", 0);
            direction = data.Attr("direction", "Right");

            Add((Component)(sprite = new Sprite(GFX.Game, "objects/pandorasBox/laser/valve/valve")));
            sprite.AddLoop("valve", "", 0.125f);
            sprite.CenterOrigin();
            sprite.Rotation = LaserHelper.DirectionRotations[direction];

            sprite.Play("valve");
            sprite.Rate = 0;

            Collider = direcitonBlockingColliders[direction];
            LaserBlockingCollider = direcitonBlockingColliders[direction];
            LaserDetectionCollider = directionDetectingColliders[direction];

            Depth = 50;

            DetectionDelay = delay;

            UpdateSprite(0);
        }

        public void UpdateSprite(int frame)
        {
            sprite.SetAnimationFrame(frame);
        }

        public override Collider GetLaserBlockingCollider(Laserbeam beam)
        {
            bool notBlocking = knownBeams != null && knownBeams.Contains(beam);

            return notBlocking ? direcitonKnownBlockingColliders[direction] : direcitonBlockingColliders[direction];
        }

        public override void OnLaserbeams(List<Laserbeam> laserbeams)
        {
            if (laserbeamPaq.Peek(0).Count == 0)
            {
                UpdateSprite(0);
            }
            else if (laserbeams.Count > 0)
            {
                UpdateSprite(2);
            }
            else
            {
                UpdateSprite(1);
            }

            knownBeams = laserbeams;
        }
    }
}
