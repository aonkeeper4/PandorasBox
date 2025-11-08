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
    [CustomEntity("pandorasBox/lamp")]
    class Lamp : Actor
    {
        private Sprite startupSprite;
        private Sprite idleSprite;
        private Sprite baseSprite;

        bool inStartupAnimation;
        bool inIdleAnimation;

        private BloomPoint bloom;
        private VertexLight light;

        private int id;
        private string flag;
        private Color baseColor;
        private Color lightColor;
        private string lightMode;
        private int lightStartRadius;
        private int lightEndRadius;
        private bool inverted;

        private float startupLerpAcc = 0f;
        private float startupLerpTotal = 0f;

        private static float bloomStartRadius = 3f;
        private static float bloomEndRadius = 8f;

        private static float lightStartAlpha = 0.15f;
        private static float lightEndAlpha = 1f;

        private static float startupAnimationDelay = 0.1f;

        public Lamp(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            id = data.ID;
            flag = data.Attr("flag", "");
            baseColor = ColorHelper.GetColor(data.Attr("baseColor", "White"));
            lightColor = ColorHelper.GetColor(data.Attr("lightColor", "White"));
            lightStartRadius = data.Int("lightStartRadius", 48);
            lightEndRadius = data.Int("lightEndRadius", 64);
            inverted = data.Bool("inverted", false);

            Add((Component)(startupSprite = new Sprite(GFX.Game, "objects/pandorasBox/lamp/start")));
            startupSprite.AddLoop("start", "", startupAnimationDelay);
            startupSprite.JustifyOrigin(0.5f, 0.5f);
            startupSprite.Play("start");
            startupSprite.OnLastFrame = onLastFrame;
            startupLerpTotal = startupAnimationDelay * startupSprite.CurrentAnimationTotalFrames;
            startupSprite.Stop();

            Add((Component)(idleSprite = new Sprite(GFX.Game, "objects/pandorasBox/lamp/idle")));
            idleSprite.AddLoop("idle", "", 0.125f);
            idleSprite.JustifyOrigin(0.5f, 0.5f);
            idleSprite.Play("idle");
            idleSprite.Visible = false;

            Add((Component)(baseSprite = new Sprite(GFX.Game, "objects/pandorasBox/lamp/base")));
            baseSprite.AddLoop("base", "", 0.1f);
            baseSprite.JustifyOrigin(0.5f, 0.5f);
            baseSprite.Play("base");
            baseSprite.SetColor(baseColor);

            inStartupAnimation = false;

            Add((Component)(bloom = new BloomPoint(0.5f, 8f)));
            Add((Component)(light = new VertexLight(lightColor, 1f, lightStartRadius, lightEndRadius)));

            Depth = 5;
        }

        public override void Awake(Scene scene)
        {
            if (isActive())
            {
                bloom.Radius = bloomEndRadius;
                light.Alpha = lightEndAlpha;
            }
            else
            {
                bloom.Radius = bloomStartRadius;
                light.Alpha = lightStartAlpha;
            }
        }

        private void onLastFrame(string s)
        {
            if (isActive())
            {
                idleSprite.Play("idle", true);
                idleSprite.Rate = 1;
                idleSprite.Visible = true;
                startupSprite.Visible = false;

                inIdleAnimation = true;
            }
            else
            {
                inIdleAnimation = false;
            }

            inStartupAnimation = false;
            startupSprite.Stop();
        }

        private bool isActive()
        {
            Level level = Scene as Level;

            if (inverted)
            {
                return !level.Session.GetFlag(flag);
            }
            else
            {
                return level.Session.GetFlag(flag);
            }
        }

        public override void Update()
        {

            if (isActive())
            {
                if (!inStartupAnimation && !inIdleAnimation)
                {
                    startupSprite.Play("start", true);
                    startupSprite.Rate = 1;
                    startupSprite.Visible = true;
                    idleSprite.Visible = false;

                    inStartupAnimation = true;
                    inIdleAnimation = false;
                    startupLerpAcc = 0f;
                }
            }
            else
            {
                if (!inStartupAnimation && inIdleAnimation)
                {
                    startupSprite.Play("start", true);
                    startupSprite.Rate = -1;
                    startupSprite.SetAnimationFrame(startupSprite.CurrentAnimationTotalFrames - 1);
                    startupSprite.Visible = true;
                    idleSprite.Visible = false;

                    inStartupAnimation = true;
                    inIdleAnimation = false;
                    startupLerpAcc = startupLerpTotal;
                }
            }

            float lerpPercent = isActive() ? 1.0f : 0.0f;

            if (inStartupAnimation && lightMode == "Smooth")
            {
                startupLerpAcc = MathHelper.Clamp(startupLerpAcc + Engine.DeltaTime * startupSprite.Rate, 0f, startupLerpTotal);
                lerpPercent = startupLerpAcc / startupLerpTotal;
            }

            bloom.Radius = MathHelper.Lerp(bloomStartRadius, bloomEndRadius, lerpPercent);
            light.Alpha = MathHelper.Lerp(lightStartAlpha, lightEndAlpha, lerpPercent);

            base.Update();
        }
    }
}
