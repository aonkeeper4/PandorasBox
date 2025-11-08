using Celeste.Mod.Entities;
using Celeste.Mod.PandorasBox;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/laserSensor")]
    class LaserSensor : LaserDetectorActor
    {
        private Color activationColor;

        private bool active;
        private bool blockLight;
        private string flag;
        private int id;
        private string mode;

        private Monocle.Image orbSprite;
        private Monocle.Image metalRingSprite;
        private Monocle.Image lightRingSprite;

        private Entity orbEntity;

        public LaserSensor(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            active = false;
            flag = data.Attr("flag", "");
            mode = data.Attr("mode", "Continuous");
            blockLight = bool.Parse(data.Attr("blockLight", "true"));
            activationColor = ColorHelper.GetColor(data.Attr("color", "#87CEFA"));
            id = data.ID;

            orbSprite = new Monocle.Image(GFX.Game["objects/pandorasBox/laser/sensor/orb"]);
            orbSprite.Origin = new Vector2(orbSprite.Width / 2f, orbSprite.Height / 2f);
            orbEntity = new Entity(Position);
            orbEntity.Add(orbSprite);
            orbEntity.Depth = 50;

            metalRingSprite = new Monocle.Image(GFX.Game["objects/pandorasBox/laser/sensor/metal_ring"]);
            metalRingSprite.Origin = new Vector2(metalRingSprite.Width / 2f, metalRingSprite.Height / 2f);
            lightRingSprite = new Monocle.Image(GFX.Game["objects/pandorasBox/laser/sensor/light_ring"]);
            lightRingSprite.Origin = new Vector2(lightRingSprite.Width / 2f, lightRingSprite.Height / 2f);
            lightRingSprite.SetColor(activationColor);
            Add(metalRingSprite);
            Add(lightRingSprite);
            Depth = 200;

            LaserDetectionCollider = new Hitbox(8f, 8f, -4f, -4f);

            if (blockLight)
            {
                LaserBlockingCollider = new Hitbox(4f, 4f, -2f, -2f);
            }
        }

        public override void Added(Scene scene)
        {
            scene.Add(orbEntity);

            base.Added(scene);
        }

        public override void OnLaserbeams(List<Laserbeam> laserbeams)
        {
            foreach (Laserbeam beam in laserbeams)
            {
                if (beam.Color.Equals(activationColor)) {
                    active = true;
                    setFlag();

                    return;
                }
            }

            if (mode == "Continuous")
            {
                active = false;
                setFlag();
            }
        }

        public override void Update()
        {
            if (mode == "HitOnce" && !getFlag()) {
                active = false;
            }

            if (mode == "Continuous")
            {
                if (getFlag() != active)
                {
                    setFlag();
                }
            }

            base.Update();
        }

        private void setFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_laser_sensor_" + id : flag;
                level.Session.SetFlag(targetFlag, active);
            }
        }

        private bool getFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_lever_" + id : flag;

                return level.Session.GetFlag(targetFlag);
            }

            return false;
        }
    }
}
