using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/dustSpriteColorController")]
    class DustSpriteColorController : Entity
    {
        private Color eyeColor;
        private string eyeTexture;
        private Vector3[] borderColors;
        private DustStyles.DustStyle style;

        public DustSpriteColorController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            eyeColor = ColorHelper.GetColor(data.Attr("eyeColor", "Red"));
            eyeTexture = data.Attr("eyeTexture", "danger/dustcreature/eyes");

            String rawColor = data.Attr("borderColor", "Green");
            List<Vector3> colors = new List<Vector3>();

            foreach (String s in rawColor.Split(','))
            {
                colors.Add(ColorHelper.GetColor(s).ToVector3());
            }

            borderColors = colors.ToArray();

            style = new DustStyles.DustStyle
            {
                EdgeColors = borderColors,
                EyeColor = eyeColor,
                EyeTextures = eyeTexture
            };
        }

        public override void Added(Scene scene)
        {
            Level level = scene as Level;
            int areaId = level.Session.Area.ID;

            if (!DustStyles.Styles.ContainsKey(areaId) || !DustStyles.Styles[areaId].Equals(style))
            {
                DustStyles.Styles[areaId] = style;
            }

            base.Added(scene);
        }

        private void removeStyle(Scene scene)
        {
            Level level = scene as Level;
            int areaId = level.Session.Area.ID;

            if (level.Session.Area.GetLevelSet() != "Celeste" && DustStyles.Styles[areaId].Equals(style))
            {
                DustStyles.Styles.Remove(areaId);
            }
        }

        public override void Removed(Scene scene)
        {
            removeStyle(scene);

            base.Remove();
        }

        public override void SceneEnd(Scene scene)
        {
            removeStyle(scene);

            base.SceneEnd(scene);
        }
    }
}
