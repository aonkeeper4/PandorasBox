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
    [CustomEntity("pandorasBox/lever")]
    class Lever : Actor
    {
        private Sprite sprite;
        private bool active;
        private string flag;
        private int id;
        private TalkComponent talker;

        private static readonly float activationDistance = 20f;

        public Lever(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Add((Component)(sprite = new Sprite(GFX.Game, "objects/pandorasBox/lever/lever")));
            sprite.AddLoop("lever", "", 0.125f);
            sprite.JustifyOrigin(0.5f, 1f);
            sprite.Play("lever");
            sprite.OnLastFrame = onLastFrame;
            sprite.Stop();

            active = data.Bool("active", false);
            flag = data.Attr("flag", "");
            id = data.ID;

            Collider = new Hitbox(sprite.Width, sprite.Height, -sprite.Width / 2, -sprite.Height);

            Add(talker = new TalkComponent(new Rectangle((int)(-sprite.Width / 2 - 2), -4, (int)(sprite.Width + 4), 4), new Vector2(0.0f, -18f), onTalk));
            talker.Enabled = true;
        }

        private void onLastFrame(string s)
        {
            sprite.Stop();
        }

        private void setFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_lever_" + id : flag;
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

        private void animate()
        {
            if (active)
            {
                sprite.Play("lever", true);
                sprite.Rate = 1;
            }
            else
            {
                sprite.Play("lever", true);
                sprite.Rate = -1;
                sprite.SetAnimationFrame(sprite.CurrentAnimationTotalFrames - 1);
            }
        }

        // So we don't animate into the correct state
        private void setCorrectStartFrame()
        {
            if (active)
            {
                sprite.Play("lever", true);
                sprite.SetAnimationFrame(sprite.CurrentAnimationTotalFrames - 1);
                sprite.Rate = 1;
            }
            else
            {
                sprite.Play("lever", true);
                sprite.Rate = -1;
                sprite.SetAnimationFrame(0);
            }
        }

        private void onTalk(Player player)
        {
            if (!sprite.Animating)
            {
                active = !active;
                setFlag();
                animate();
            }
        }

        public override void Update()
        {
            talker.Enabled = Scene.Tracker.GetEntities<Player>().Cast<Player>().Any(player => (Position - player.Position).Length() < activationDistance && player.OnGround() && !PlayerHelper.PlayerInWater(player));

            bool flagValue = getFlag();
            if (flagValue != active && !sprite.Animating)
            {
                active = flagValue;
                animate();
            }

            base.Update();
        }

        public override void Added(Scene scene)
        {
            setCorrectStartFrame();
            setFlag();

            base.Added(scene);
        }
    }
}
