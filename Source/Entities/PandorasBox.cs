using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.PandorasBox
{
    [CustomEntity("pandorasBox/pandorasBox")]
    class PandorasBox : Actor
    {
        private TalkComponent talker;
        private bool canTalk;
        private bool ruiningTheWorld;

        private bool completeChapter;
        private string dialogId;

        private Sprite boxIdle;
        private Sprite boxOpen;

        private float effectAcc;

        private TileGlitcher tileGlitcher;

        public PandorasBox(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Add(talker = new TalkComponent(new Rectangle(-8, -8, 16, 16), new Vector2(0.0f, -24f), onTalk));
            talker.Enabled = true;
            canTalk = true;

            Add((Component)(boxOpen = new Sprite(GFX.Game, "objects/pandorasBox/pandorasBox/box_open")));
            boxOpen.AddLoop("box_open", "", 0.25f);
            boxOpen.JustifyOrigin(0.5f, 1f);
            boxOpen.Visible = false;
            boxOpen.Stop();

            Add((Component)(boxIdle = new Sprite(GFX.Game, "objects/pandorasBox/pandorasBox/box_idle")));
            boxIdle.AddLoop("box_idle", "", 0.1f);
            boxIdle.Play("box_idle");
            boxIdle.JustifyOrigin(0.5f, 1f);
            
            completeChapter = Boolean.Parse(data.Attr("completeChapter", "false"));
            dialogId = data.Attr("dialog", "");

            Depth = 5;
        }

        private void onTalk(Player player)
        {
            Level level = Scene as Level;

            player.StateMachine.State = Player.StDummy;
            player.StateMachine.Locked = true;

            ruiningTheWorld = true;
            canTalk = false;

            Add((Component)new Coroutine(doomTheWorld(player), true));

            boxOpen.Visible = true;
            boxIdle.Visible = false;

            boxOpen.Play("box_open");
        }

        public override void Update()
        {
            Player player = base.Scene.Tracker.GetEntity<Player>();

            if (player != null)
            {
                float playerDistance = (Position - player.Position).Length();
                bool playerCloseEnough = playerDistance < 40f;
                talker.Enabled = canTalk && playerCloseEnough && player.OnGround();

                Distort.GameRate = Calc.Approach(Distort.GameRate, Calc.Map(Engine.TimeRate, 0.5f, 1f, 0f, 1f), Engine.DeltaTime * 2f);
                Distort.Anxiety = Calc.Approach(Distort.Anxiety, (0.5f + Calc.Random.Range(-0.2f, 0.2f)) * effectAcc, 8f * Engine.DeltaTime);
                Distort.AnxietyOrigin = new Vector2((player.Center.X - Position.X) / 320f, (player.Center.Y - Position.Y) / 180f);

                Glitch.Value = effectAcc / 8f;

                if (ruiningTheWorld)
                {
                    if (Scene.OnInterval(0.5f))
                    {
                        effectAcc += Calc.Random.Range(0, 0.10f);
                        effectAcc = Calc.Min(effectAcc, 7.5f);
                    }
                }
                else
                {
                    effectAcc = playerCloseEnough ? 0.3f - (playerDistance + 0.5f) / 160f : 0f;
                }
            }

            base.Update();
        }

        private List<string> getDialogOptions()
        {
            List<string> res = new List<string>();

            int i = 0;
            while (Dialog.Has("PANDORAS_BOX_POSTCARD_" + i))
            {
                res.Add("PANDORAS_BOX_POSTCARD_" + i);
                i++;
            }

            return res;
        }

        private List<Entity> getRandomEntities(Level level, float threshold)
        {
            List<Entity> res = new List<Entity>();

            foreach (Entity entity in level.Entities)
            {
                if (Calc.Random.NextFloat() < threshold)
                {
                    res.Add(entity);
                }
            }

            return res;
        }

        private string getPoemSoundPrefix(string soundId)
        {
            // Based on Everest implementation
            if (string.IsNullOrEmpty(soundId))
            {
                soundId = "csides";
            }

            string prefix;
            if (soundId.StartsWith("event:/"))
            {
                // sound ID is a FMOD event, take it as is.
                prefix = soundId;
            }
            else if (soundId == "variants")
            {
                // sound ID is "variants", this is a special case since it is in the new_content bank.
                prefix = "event:/new_content/ui/postcard_variants";
            }
            else
            {
                // if a number, use event:/ui/main/postcard_ch{number}
                // if not, use event:/ui/main/postcard_{text}
                prefix = "event:/ui/main/postcard_";

                if (int.TryParse(soundId, out _))
                {
                    prefix += "ch";
                }

                prefix += soundId;
            }

            return prefix;
        }

        private IEnumerator doomTheWorld(Player player)
        {
            Level level = Scene as Level;

            level.PauseLock = true;

            if (completeChapter)
            {
                level.TimerStopped = true;
                level.RegisterAreaComplete();
            }

            while (boxOpen.CurrentAnimationFrame != boxOpen.CurrentAnimationTotalFrames - 1)
            {
                yield return 0.25f;
            }

            boxOpen.Stop();

            tileGlitcher = new TileGlitcher(new Vector2(level.Bounds.Left, level.Bounds.Top), level.Bounds.Width, level.Bounds.Height, true, false, "Both", 0.125f, 0.05f, "", "", "");
            level.Add(tileGlitcher);

            for (int i = 0; i < Calc.Random.Range(30f, 50f); i++)
            {
                List<Entity> targets = getRandomEntities(level, 0.075f);

                foreach (Entity target in targets)
                {
                    if (target != player && target != level.SolidTiles && target != level.BgTiles && target != this && target != tileGlitcher && !(target is IStrawberry))
                    {
                        target.RemoveSelf();
                    }
                }

                yield return 0.2f;
            }
         
            ruiningTheWorld = false;

            var areaData = AreaData.Get(level);
            string postcardSoundId = areaData?.Meta?.PostcardSoundID;
            string soundPrefix = getPoemSoundPrefix(postcardSoundId);

            Postcard postcard;

            if (string.IsNullOrEmpty(dialogId))
            {
                List<string> options = getDialogOptions();
                string dialogId = options[Calc.Random.Next(options.Count)];
                postcard = new Postcard(Dialog.Get(dialogId), soundPrefix + "_in", soundPrefix + "_out");
            }
            else
            {
                postcard = new Postcard(Dialog.Get(dialogId), soundPrefix + "_in", soundPrefix + "_out");
            }

            Engine.Scene = new PandorasBoxPostcard(postcard, Scene, completeChapter);
        }
    }
}
