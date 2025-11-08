using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO - Use the full size sprites instead, then slice out what is needed
// Easier to match the collision box height to the actuall sprites then
// TODO - Custom texture support
namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/gate")]
    class Gate : Solid
    {
        private Sprite sprite;
        private Shaker shaker;

        static int openHeight = 8;
        static int closedHeight = 48;
        static float waitDelay = 0.2f;

        bool open;
        bool lockState;

        private float drawingHeight;
        private float drawingHeightSpeed;
        private float waitFor;

        private string flag;
        private string textureFolder;
        private bool inverted;
        private int id;

        public Gate(EntityData data, Vector2 offset) : base(data.Position + offset, 8f, 48f, true)
        {
            inverted = Boolean.Parse(data.Attr("inverted", "false"));
            textureFolder = data.Attr("texture", "objects/pandorasBox/gate/");
            flag = data.Attr("flag", "");
            id = data.ID;

            lockState = false;

            Add((Component)(sprite = new Sprite(GFX.Game, textureFolder + "gate")));
            sprite.AddLoop("gate", "", 0.06f);
            sprite.Play("gate");
            sprite.OnLastFrame = onLastFrame;
            sprite.Rate = 0;

            sprite.X = -base.Collider.Width / 2f;

            Add(shaker = new Shaker(on: false));

            Depth = -9000;

            drawingHeight = sprite.Height;
        }

        private void onLastFrame(string s)
        {
            sprite.Stop();
        }

        private bool isActive()
        {
            Level level = Scene as Level;

            return inverted ? !level.Session.GetFlag(flag) : level.Session.GetFlag(flag);
        }

        public override void Update()
        {

            if (!lockState)
            {
                if (isActive())
                {
                    if (!open)
                    {
                        Open();
                    }
                }
                else
                {
                    if (open)
                    {
                        Close();
                    }
                }
            }

            waitFor = Math.Max(0, waitFor - Engine.DeltaTime);

            if (waitFor <= 0)
            {
                float heightTarget = open ? openHeight : closedHeight;
                SetHeight((int)heightTarget);

                float drawingTarget = open ? Math.Max(openHeight, Height) : Math.Min(closedHeight, Height);
                if (drawingHeight != drawingTarget)
                {
                    drawingHeight = Calc.Approach(drawingHeight, drawingTarget, drawingHeightSpeed * Engine.DeltaTime);
                }

            }

            base.Update();
        }

        private void SetHeight(int height)
        {
            if (height == Collider.Height)
            {
                return;
            }

            if (height < Collider.Height)
            {
                Collider.Height = height;

                return;
            }

            float y = Y;
            int num = (int)Collider.Height;

            if (Collider.Height < 64f)
            {
                Y -= 64f - Collider.Height;
                Collider.Height = 64f;
            }

            MoveVExact(height - num);
            Y = y;
            Collider.Height = height;
        }

        public void Open()
        {
            Audio.Play("event:/game/05_mirror_temple/gate_main_open", Position);
            shaker.ShakeFor(0.2f, removeOnFinish: false);

            sprite.Play("gate", true);
            sprite.Rate = 1f;

            shaker.ShakeFor(0.2f, removeOnFinish: false);

            drawingHeightSpeed = 200f;
            waitFor = waitDelay;

            open = true;
        }

        public void Close()
        {
            Audio.Play("event:/game/05_mirror_temple/gate_main_close", Position);
            shaker.ShakeFor(0.2f, removeOnFinish: false);

            sprite.Play("gate", true);
            sprite.Rate = -1f;
            sprite.SetAnimationFrame(sprite.CurrentAnimationTotalFrames - 1);

            shaker.ShakeFor(0.2f, removeOnFinish: false);

            drawingHeightSpeed = 300f;
            waitFor = waitDelay;

            open = false;
        }

        public override void Render()
        {
            Vector2 shakerOffset = new Vector2(Math.Sign(shaker.Value.X), 0f);
            sprite.DrawSubrect(shakerOffset, new Rectangle(0, (int)(sprite.Height - drawingHeight), (int)sprite.Width, (int)drawingHeight + 1));
        }
    }
}
