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
    [CustomEntity("pandorasBox/laserWallSensor")]
    class LaserWallSensor : LaserDetectorActor
    {
        private string direction;
        private string flag;
        private int id;

        private Sprite sprite;

        private bool active;

        public LaserWallSensor(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Add((Component)(sprite = new Sprite(GFX.Game, "objects/pandorasBox/laser/sensor/sensor")));
            sprite.AddLoop("sensor", "", 0.125f);
            sprite.CenterOrigin();
            sprite.Play("sensor");

            flag = data.Attr("flag", "");
            direction = data.Attr("direction", "Left");

            id = data.ID;

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            base.LaserDetectionCollider = new Hitbox(16f, 16f, -8f, -8f);
        }

        public override void OnLaserbeams(List<Laserbeam> laserbeams)
        {
            active = laserbeams.Count > 0;
            setFlag();
        }

        private void setFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_laser_detector" + id : flag;
                level.Session.SetFlag(targetFlag, active);
            }
        }
    }
}
