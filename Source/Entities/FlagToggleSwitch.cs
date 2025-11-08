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
    [CustomEntity("pandorasBox/flagToggleSwitch")]
    class FlagToggleSwitch : Entity
    {
        private const float Cooldown = 1f;

        private bool flagActive;

        private float cooldownTimer;

        private bool onlyOn;
        private bool onlyOff;
        private string flag;
        private int id;
        private bool silent;

        private bool playSounds;

        private Sprite sprite;

        private static SpriteBank spriteBank;

        private bool Usable
        {
            get
            {
                if (onlyOff && !flagActive || onlyOn && flagActive)
                {
                    return false;
                }

                return true;
            }
        }

        public FlagToggleSwitch(Vector2 position, int id, bool onlyOn, bool onlyOff, bool silent, string flag)
            : base(position)
        {
            this.id = id;
            this.onlyOn = onlyOn;
            this.onlyOff = onlyOff;
            this.flag = flag;
            this.silent = silent;

            Collider = new Hitbox(16f, 24f, -8f, -12f);

            Add(new PlayerCollider(OnPlayer));
            Add(sprite = spriteBank.Create("pandorasBoxFlagTooggleSwitch"));
            //Add(sprite = GFX.SpriteBank.Create("coreFlipSwitch"));

            Depth = 2000;
        }

        public FlagToggleSwitch(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.ID, data.Bool("onlyOn"), data.Bool("onlyOff"), data.Bool("silent"), data.Attr("flag"))
        {
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            flagActive = getFlag();
            SetSprite(false);
        }

        private void SetSprite(bool animate)
        {
            if (animate)
            {
                if (playSounds)
                {
                    Audio.Play(flagActive ? "event:/game/09_core/switch_to_hot" : "event:/game/09_core/switch_to_cold", Position);
                }

                if (Usable)
                {
                    sprite.Play(flagActive ? "activate" : "deactivate");
                }
                else
                {
                    if (playSounds)
                    {
                        Audio.Play("event:/game/09_core/switch_dies", Position);
                    }
                    sprite.Play(flagActive ? "activateOff" : "deactivateOff");
                }
            }
            else if (Usable)
            {
                sprite.Play(flagActive ? "activateLoop" : "deactivateLoop");
            }
            else
            {
                sprite.Play(flagActive ? "activateOffLoop" : "deactivateOffLoop");
            }

            playSounds = false;
        }

        private void OnPlayer(Player player)
        {
            if (Usable && cooldownTimer <= 0f)
            {
                playSounds = !silent;
                flagActive = !flagActive;
                setFlag();
                SetSprite(true);

                Level level = SceneAs<Level>();

                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                level.Flash(Color.White * 0.15f, drawPlayerOver: true);
                Celeste.Freeze(0.05f);
                cooldownTimer = 1f;
            }
        }

        public override void Update()
        {
            base.Update();

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Engine.DeltaTime;
            }

            bool currentFlag = getFlag();

            if (currentFlag != flagActive)
            {
                flagActive = currentFlag;
                SetSprite(true);
            }
        }

        private void setFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_flagtoggleswitch_" + id : flag;
                level.Session.SetFlag(targetFlag, flagActive);
            }
        }

        private bool getFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_flagtoggleswitch_" + id : flag;

                return level.Session.GetFlag(targetFlag);
            }

            return false;
        }

        public static void LoadContent()
        {
            spriteBank = new SpriteBank(GFX.Game, "Graphics/PandorasBox/flagToggleSwitch.xml");
        }
    }
}
