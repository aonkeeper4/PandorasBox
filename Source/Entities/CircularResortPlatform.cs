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
    [CustomEntity("pandorasBox/circularResortPlatform")]
    class CircularResortPlatform : JumpThru
    {
        private float rotationPercent;
        private Vector2 startCenter;
        private Vector2 startPosition;

        private bool clockwise;
        private float speed;
        private bool hasParticles;
        private bool attachToSolid;
        private string texture;
        private float width;
        private bool renderRail;

        private Color lineFillColor;
        private Color lineEdgeColor;

        private CircularResortPlatformRail rail;

        private float length;
        private float addY;
        private float sinkTimer;
        private bool fallOutOfScreen;

        private MTexture[] textures;

        public float Angle
        {
            get
            {
                return MathHelper.Lerp(3.14159274f, -3.14159274f, rotationPercent);
            }
        }

        public CircularResortPlatform(Vector2 position, int width, Vector2 node, string texture, bool clockwise, float speed, bool particles, bool attachToSolid, bool renderRail, Color lineFillColor, Color lineEdgeColor, bool rotationFix) : base(position, width, false)
        {
            startCenter = node - new Vector2(width / 2f, 4);
            startPosition = position;

            // Make sure no old maps break
            if (rotationFix)
            {
                float angleRadians = Calc.Angle(startCenter, Position);
                angleRadians = Calc.WrapAngle(angleRadians);
                rotationPercent = Calc.Percent(angleRadians, 3.14159274f, -3.14159274f);

                length = (this.Position - startCenter).Length();
                MoveTo(startCenter + Calc.AngleToVector(angleRadians, length));
            }
            else
            {
                float num = Calc.Angle(startCenter, startPosition);
                num = Calc.WrapAngle(num);
                float angle = MathHelper.Lerp(4.712389f, -1.57079637f, this.rotationPercent);
                rotationPercent = Calc.Percent(num, 4.712389f, 3.1415925f);

                length = (this.Position - startCenter).Length();
                MoveTo(startCenter + Calc.AngleToVector(num, length));
            }


            this.clockwise = clockwise;
            this.speed = speed;
            this.hasParticles = particles;
            this.attachToSolid = attachToSolid;
            this.texture = texture;
            this.width = width;

            if (String.IsNullOrEmpty(texture))
            {
                texture = AreaData.Get(Scene).WoodPlatform;
            }

            this.renderRail = renderRail;
            this.lineFillColor = lineFillColor;
            this.lineEdgeColor = lineEdgeColor;

            Add(new LightOcclude(0.2f));

            if (attachToSolid)
            {
                Add(new StaticMover
                {
                    SolidChecker = ((Solid s) => s.CollidePoint(startCenter + new Vector2(width / 2f, 4))),
                    JumpThruChecker = ((JumpThru jt) => jt != this && jt.CollidePoint(startCenter + new Vector2(width / 2f, 4))),
                    OnMove = delegate (Vector2 v)
                    {
                        startCenter += v;
                        rail.Position += v;
                    },
                    OnDestroy = delegate()
                    {
                        fallOutOfScreen = true;
                    }
                });
            }
        }

        public CircularResortPlatform(EntityData data, Vector2 offset) : this(data.Position + offset, data.Width, data.Nodes[0] + offset, data.Attr("texture", ""), data.Bool("clockwise", true), data.Float("speed", 1500f), data.Bool("particles", true), data.Bool("attachToSolid", true), data.Bool("renderRail", true), ColorHelper.GetColor(data.Attr("lineFillColor", "160b12")), ColorHelper.GetColor(data.Attr("lineEdgeColor", "2a1923")), data.Bool("rotationFix", false))
        {

        }

        public override void Update()
        {
            bool hasRider = base.HasPlayerRider();

            if (hasRider)
            {
                sinkTimer = 0.2f;
                addY = Calc.Approach(addY, 3f, 50f * Engine.DeltaTime);
            }
            else
            {
                bool sinking = sinkTimer > 0f;
                if (sinking)
                {
                    sinkTimer -= Engine.DeltaTime;
                    addY = Calc.Approach(addY, 3f, 50f * Engine.DeltaTime);
                }
                else
                {
                    addY = Calc.Approach(addY, 0f, 20f * Engine.DeltaTime);
                }
            }

            if (hasParticles && base.Scene.OnInterval(0.02f))
            {
                base.SceneAs<Level>().ParticlesBG.Emit(DustStaticSpinner.P_Move, 1, this.Position + new Vector2(Width / 2, 4), Vector2.One * 4f);
            }

            if (clockwise)
            {
                this.rotationPercent -= speed * Engine.DeltaTime / 2777.8f;
                this.rotationPercent += 1f;
            }
            else
            {
                this.rotationPercent += speed * Engine.DeltaTime / 2777.8f;
            }

            if (fallOutOfScreen)
            {
                startCenter.Y = startCenter.Y + 160f * Engine.DeltaTime;
                if (Y > (base.Scene as Level).Bounds.Bottom + 32)
                {
                    RemoveSelf();
                }
            }

            rotationPercent %= 1f;
            Vector2 newPosition = startCenter + Calc.AngleToVector(Angle, length) + Vector2.UnitY * addY;
            MoveTo(newPosition);

            base.Update();
        }

        public override void Render()
        {
            textures[0].Draw(this.Position);

            int num = 8;
            while (num <= width - 8f)
            {
                textures[1].Draw(Position + new Vector2(num, 0f));
                num += 8;
            }

            textures[3].Draw(Position + new Vector2(width - 8f, 0f));
            textures[2].Draw(Position + new Vector2(width / 2f - 4f, 0f));
        }

        public override void Added(Scene scene)
        {
            MTexture mtexture = GFX.Game["objects/woodPlatform/" + texture];
            this.textures = new MTexture[mtexture.Width / 8];
            for (int i = 0; i < this.textures.Length; i++)
            {
                this.textures[i] = mtexture.GetSubtexture(i * 8, 0, 8, 8, null);
            }

            if (renderRail)
            {
                scene.Add(rail = new CircularResortPlatformRail(startCenter, lineEdgeColor, lineFillColor, width, length));
            }

            base.Added(scene);
        }
    }
}
