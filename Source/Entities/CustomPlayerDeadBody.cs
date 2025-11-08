using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.PandorasBox
{
    public class CustomPlayerDeadBody : Entity
    {
        public Action DeathAction;
        public float ActionDelay;

        public bool HasGolden;
        private Color initialHairColor;
        private Vector2 bounce = Vector2.Zero;
        private Player player;
        private PlayerHair hair;
        private PlayerSprite sprite;
        private Facings facing;

        private VertexLight light;
        private DeathEffect deathEffect;
        private float scale = 1f;
        private bool finished;
        private bool endLevel;
        private bool muted;

        public CustomPlayerDeadBody(Player player, Vector2 direction, bool endLevel=false, bool muted=true)
        {
            base.Depth = -1000000;

            this.player = player;
            this.endLevel = endLevel;
            this.muted = muted;

            facing = player.Facing;
            Position = player.Position;

            Add(hair = player.Hair);
            Add(sprite = player.Sprite);
            Add(light = player.Light);

            sprite.Color = Color.White;
            initialHairColor = hair.Color;
            bounce = direction;

            Add(new Coroutine(DeathRoutine()));
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            if (bounce == Vector2.Zero)
            {
                return;
            }

            if (Math.Abs(bounce.X) > Math.Abs(bounce.Y))
            {
                sprite.Play("deadside");
                facing = (Facings)(-Math.Sign(bounce.X));

                return;
            }

            bounce = Calc.AngleToVector(Calc.AngleApproach(bounce.Angle(), new Vector2(0 - player.Facing, 0f).Angle(), 0.5f), 1f);

            if (bounce.Y < 0f)
            {
                sprite.Play("deadup");
            }
            else
            {
                sprite.Play("deaddown");
            }
        }

        private void playAudio(string name, Vector2 position) {
            if (!muted)
            {
                Audio.Play(name, position);
            }
        }

        private IEnumerator DeathRoutine()
        {
            Level level = SceneAs<Level>();
            if (bounce != Vector2.Zero)
            {
                scale = 1.5f;

                playAudio("event:/char/madeline/predeath", Position);
                Celeste.Freeze(0.05f);

                yield return null;

                Vector2 from = Position;
                Vector2 to = from + bounce * 24f;

                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 0.5f, start: true);
                Add(tween);

                tween.OnUpdate = delegate (Tween t)
                {
                    Position = from + (to - from) * t.Eased;
                    scale = 1.5f - t.Eased * 0.5f;
                    sprite.Rotation = (float)(Math.Floor(t.Eased * 4f) * 6.2831854820251465);
                };

                yield return tween.Duration * 0.75f;

                tween.Stop();
            }
      
            Position += Vector2.UnitY * -5f;

            level.Displacement.AddBurst(Position, 0.3f, 0f, 80f);
            level.Shake();
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);

            playAudio(HasGolden ? "event:/new_content/char/madeline/death_golden" : "event:/char/madeline/death", Position);

            deathEffect = new DeathEffect(initialHairColor, Center - Position);
            deathEffect.OnUpdate = delegate (float f)
            {
                light.Alpha = 1f - f;
            };
            Add(deathEffect);

            yield return deathEffect.Duration * 0.65f;

            if (ActionDelay > 0f)
            {
                yield return ActionDelay;
            }

            End();
        }

        private void End()
        {
            if (!finished && endLevel)
            {
                finished = true;
                Level level = SceneAs<Level>();

                if (DeathAction == null)
                {
                    DeathAction = level.Reload;
                }

                level.DoScreenWipe(false, DeathAction);
            }
        }

        public override void Update()
        {
            base.Update();

            if (Input.MenuConfirm.Pressed && !finished)
            {
                End();
            }

            hair.Color = ((sprite.CurrentAnimationFrame == 0) ? Color.White : initialHairColor);
        }

        public override void Render()
        {
            if (deathEffect == null)
            {
                sprite.Scale.X = (float)facing * scale;
                sprite.Scale.Y = scale;
                hair.Facing = facing;

                base.Render();
            }
            else
            {
                deathEffect.Render();
            }
        }
    }

}
