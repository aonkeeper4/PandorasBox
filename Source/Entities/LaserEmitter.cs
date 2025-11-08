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
    [CustomEntity("pandorasBox/laserEmitter")]
    class LaserEmitter : Actor
    {
        private Sprite startupSprite;
        private Sprite idleSprite;
        private Laserbeam laserbeam;
        private BloomPoint bloom;
        private VertexLight light;

        private string flag;
        private string direction;
        private int beamDuration;
        private bool inverted;
        private int id;
        private Color color;
        private bool inStartupAnimation;

        private static Vector2 laserOffset = new Vector2(0f, 25f);

        public LaserEmitter(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Add((Component)(startupSprite = new Sprite(GFX.Game, "objects/pandorasBox/laser/emitter/start")));
            startupSprite.AddLoop("start", "", 0.1f);
            startupSprite.JustifyOrigin(0.5f, 1f);
            startupSprite.Play("start");
            startupSprite.OnLastFrame = onLastFrame;
            startupSprite.Stop();

            Add((Component)(idleSprite = new Sprite(GFX.Game, "objects/pandorasBox/laser/emitter/idle")));
            idleSprite.AddLoop("idle", "", 0.125f);
            idleSprite.JustifyOrigin(0.5f, 1f);
            idleSprite.Play("idle");
            idleSprite.Visible = false;

            flag = data.Attr("flag", "");
            direction = data.Attr("direction", "");
            beamDuration = data.Int("beamDuration", -1);
            inverted = Boolean.Parse(data.Attr("inverted", "false"));
            id = data.ID;
            color = ColorHelper.GetColor(data.Attr("color", "White"));

            inStartupAnimation = false;

            Collider = new Hitbox(16f, 32f, -8f, -32f);

            Depth = 50;

            Add((Component)(bloom = new BloomPoint(new Vector2(0, -24), 0.3f, 4f)));
            Add((Component)(light = new VertexLight(new Vector2(0, -24), Color.White, 1f, 48, 64)));

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
                    RemoveSelf();
                }
            });
        }

        private void onLastFrame(string s)
        {
            if (isActive() && laserbeam == null) {
                Level level = Scene as Level;

                laserbeam = new Laserbeam(Position - laserOffset, direction, color, beamDuration, this);
                level.Add(laserbeam);

                idleSprite.Play("idle", true);
                idleSprite.Rate = 1;
                idleSprite.Visible = true;
                startupSprite.Visible = false;

                bloom.Visible = light.Visible = true;
            }

            inStartupAnimation = false;
            startupSprite.Stop();
        }

        private bool isActive()
        {
            Level level = Scene as Level;

            return inverted ? !level.Session.GetFlag(flag) : level.Session.GetFlag(flag);
        }

        public override void Update()
        {
            if (isActive())
            {
                if (laserbeam == null && !inStartupAnimation)
                {
                    startupSprite.Play("start", true);
                    startupSprite.Rate = 1;
                    startupSprite.Visible = true;
                    idleSprite.Visible = false;

                    inStartupAnimation = true;

                    bloom.Visible = light.Visible = true;
                }
            }
            else
            {
                if (laserbeam != null && !inStartupAnimation)
                {
                    startupSprite.Play("start", true);
                    startupSprite.Rate = -1;
                    startupSprite.SetAnimationFrame(startupSprite.CurrentAnimationTotalFrames - 1);
                    startupSprite.Visible = true;
                    idleSprite.Visible = false;

                    inStartupAnimation = true;

                    laserbeam.RemoveSelf();
                    laserbeam = null;

                    bloom.Visible = light.Visible = false;
                }
            }

            base.Update();
        }
    }
}
