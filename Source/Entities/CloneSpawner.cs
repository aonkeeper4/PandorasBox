using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using MonoMod;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.PandorasBox
{
    [Tracked]
    [CustomEntity("pandorasBox/playerClone")]
    class CloneSpawner : Actor
    {
        public Player Clone;

        private string flag;
        private int id;
        private bool active;
        private string visualMode;

        private static FieldInfo spinnerOffset = typeof(CrystalStaticSpinner).GetField("offset", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo playerRespawnTween = typeof(Player).GetField("respawnTween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo virtualButtonConsumed = typeof(VirtualButton).GetField("consumed", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo virtualButtonBufferCounter = typeof(VirtualButton).GetField("bufferCounter", BindingFlags.Instance | BindingFlags.NonPublic);

        private static ConditionalWeakTable<CrystalStaticSpinner, ValueHolder<float>> spinnerOffsets = new ConditionalWeakTable<CrystalStaticSpinner, ValueHolder<float>>();
        private static ConditionalWeakTable<Player, Player> syncedRespawn = new ConditionalWeakTable<Player, Player>();

        public static Dictionary<VirtualButton, bool> ConsumedPresses = new Dictionary<VirtualButton, bool>();
        public static Dictionary<VirtualButton, float> BufferCounters = new Dictionary<VirtualButton, float>();

        public static Player closestLookoutPlayer;

        private bool getFlag()
        {
            Level level = Scene as Level;

            if (level != null)
            {
                string targetFlag = string.IsNullOrEmpty(flag) ? "pb_clone_spawner_" + id : flag;

                return level.Session.GetFlag(targetFlag);
            }

            return false;
        }

        private void handleClone()
        {
            Level level = Scene as Level;
            List<Entity> players = Scene.Tracker.GetEntities<Player>();

            if (active)
            {
                if (Clone == null)
                {
                    Clone = PlayerCloneHelper.CreatePlayer(level, Position, visualMode);
                    level.Add(Clone);
                }
            }
            else
            {
                if (Clone != null)
                {
                    Clone.RemoveSelf();
                    Clone = null;
                }
            }
        }

        private static float getSpinnerOffset(CrystalStaticSpinner spinner)
        {
            if (spinnerOffsets.TryGetValue(spinner, out var holder))
            {
                return holder.value;
            }
            else
            {
                float offset = (float)spinnerOffset.GetValue(spinner);

                spinnerOffsets.Add(spinner, new ValueHolder<float>(offset));

                return offset;
            }
        }

        public override void Added(Scene scene)
        {
            active = getFlag();

            base.Added(scene);
        }

        public override void Removed(Scene scene)
        {
            active = false;
            handleClone();

            base.Removed(scene);
        }

        public override void Update()
        {
            active = getFlag();
            handleClone();

            base.Update();
        }

        public CloneSpawner(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            flag = data.Attr("flag");
            id = data.ID;
            visualMode = data.Attr("mode", "Inventory");
        }

        private static Hook hook_DialogCutscene_OnBegin;
        private static Hook hook_DialogCutscene_OnEnd;

        public static void Load()
        {
            hook_DialogCutscene_OnBegin = new Hook(typeof(DialogCutscene).GetMethod("OnBegin", BindingFlags.Instance | BindingFlags.Public)!, DialogCutscene_OnBegin);
            hook_DialogCutscene_OnEnd = new Hook(typeof(DialogCutscene).GetMethod("OnEnd", BindingFlags.Instance | BindingFlags.Public)!, DialogCutscene_OnEnd);

            On.Celeste.CrystalStaticSpinner.Update += CrystalStaticSpinner_Update;

            On.Celeste.Player.Die += Player_OnDie;
            On.Celeste.Player.Added += Player_Added;
            On.Celeste.Player.Update += Player_Update;

            On.Celeste.Lookout.Interact += Lookout_Interact;
            On.Celeste.Lookout.LookRoutine += Lookout_LookRoutine;
            On.Celeste.Lookout.StopInteracting += Lookout_StopInteracting;

            On.Monocle.VirtualButton.Update += VirtualButton_Update;

            IL.Celeste.TalkComponent.Update += TalkComponent_Update;

            IL.Celeste.Player.Jump += patchConsumeBuffer;
            IL.Celeste.Player.SuperJump += patchConsumeBuffer;
            IL.Celeste.Player.SuperWallJump += patchConsumeBuffer;
            IL.Celeste.Player.StartDash += patchConsumeBuffer;
            IL.Celeste.Player.SwimUpdate += patchConsumeBuffer;
            IL.Celeste.Player.HitSquashUpdate += patchConsumeBuffer;

            IL.Celeste.Player.BoostUpdate += patchConsumePress;
        }

        public static void Unload()
        {
            hook_DialogCutscene_OnBegin?.Dispose();
            hook_DialogCutscene_OnBegin = null;
            hook_DialogCutscene_OnEnd?.Dispose();
            hook_DialogCutscene_OnEnd = null;

            On.Celeste.CrystalStaticSpinner.Update -= CrystalStaticSpinner_Update;

            On.Celeste.Player.Die -= Player_OnDie;
            On.Celeste.Player.Added -= Player_Added;
            On.Celeste.Player.Update -= Player_Update;

            On.Celeste.Lookout.Interact -= Lookout_Interact;
            On.Celeste.Lookout.LookRoutine -= Lookout_LookRoutine;
            On.Celeste.Lookout.StopInteracting -= Lookout_StopInteracting;

            On.Monocle.VirtualButton.Update -= VirtualButton_Update;

            IL.Celeste.TalkComponent.Update -= TalkComponent_Update;

            IL.Celeste.Player.Jump -= patchConsumeBuffer;
            IL.Celeste.Player.SuperJump -= patchConsumeBuffer;
            IL.Celeste.Player.SuperWallJump -= patchConsumeBuffer;
            IL.Celeste.Player.StartDash -= patchConsumeBuffer;
            IL.Celeste.Player.SwimUpdate -= patchConsumeBuffer;
            IL.Celeste.Player.HitSquashUpdate -= patchConsumeBuffer;

            IL.Celeste.Player.BoostUpdate -= patchConsumePress;
        }

        private static void VirtualButton_Update(On.Monocle.VirtualButton.orig_Update orig, VirtualButton self)
        {
            ConsumedPresses.Remove(self);
            BufferCounters.Remove(self);

            orig(self);
        }

        private static void patchConsumeBuffer(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<VirtualButton>("ConsumeBuffer")))
            {
                Logger.Log($"{PandorasBoxModule.LoggerTag}/VirtualButton", $"Patching ConsumeBuffer at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Ldfld, typeof(VirtualButton).GetField("bufferCounter", BindingFlags.Instance | BindingFlags.NonPublic));

                cursor.EmitDelegate<Action<Object, float>>((object button, float bufferCounter) =>
                {
                    BufferCounters[button as VirtualButton] = bufferCounter;
                });

                cursor.Index++;
            }
        }

        private static void patchConsumePress(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<VirtualButton>("ConsumePress")))
            {
                Logger.Log($"{PandorasBoxModule.LoggerTag}/VirtualButton", $"Patching ConsumePress at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Ldfld, typeof(VirtualButton).GetField("bufferCounter", BindingFlags.Instance | BindingFlags.NonPublic));

                cursor.EmitDelegate<Action<Object, float>>((object button, float bufferCounter) =>
                {
                    BufferCounters[button as VirtualButton] = bufferCounter;
                    ConsumedPresses[button as VirtualButton] = true;
                });

                cursor.Index++;
            }
        }

        private static void CrystalStaticSpinner_Update(On.Celeste.CrystalStaticSpinner.orig_Update orig, CrystalStaticSpinner self)
        {
            bool visibleBeforeOrig = self.Visible;

            orig(self);

            if (visibleBeforeOrig && !self.Collidable && self.Scene.OnInterval(0.05f, getSpinnerOffset(self)))
            {
                var players = self.Scene.Tracker.GetEntities<Player>();

                if (players.Count > 1)
                {
                    foreach (Player entity in players)
                    {
                        self.Collidable = self.Collidable || (Math.Abs(entity.X - self.X) < 128f && Math.Abs(entity.Y - self.Y) < 128f);
                    }
                }
            }
        }

        private static void DialogCutscene_OnBegin(Action<DialogCutscene, Level> orig, DialogCutscene self, Level level)
        {
            List<Entity> players = self.Scene.Tracker.GetEntities<Player>();

            // Only if we have clones in the room
            if (players.Count > 1)
            {
                foreach (Player player in self.Scene.Tracker.GetEntities<Player>())
                {
                    player.StateMachine.State = 11;
                    player.StateMachine.Locked = true;
                    player.ForceCameraUpdate = true;
                }
            }

            orig(self, level);
        }

        private static void DialogCutscene_OnEnd(Action<DialogCutscene, Level> orig, DialogCutscene self, Level level)
        {
            List<Entity> players = self.Scene.Tracker.GetEntities<Player>();

            // Only if we have clones in the room
            if (players.Count > 1)
            {
                foreach (Player player in self.Scene.Tracker.GetEntities<Player>())
                {
                    player.StateMachine.Locked = false;
                    player.StateMachine.State = 0;
                    player.ForceCameraUpdate = false;
                }
            }

            orig(self, level);
        }

        private static PlayerDeadBody Player_OnDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible = false, bool registerDeathsInStats = true)
        {
            IEnumerable<Player> players = self.Scene.Tracker.GetEntities<Player>().Cast<Player>();
            int playerCount = players.Count();
            int deadPlayers = players.Where(p => p.Dead).Count();
            PlayerDeadBody body = self.Scene.Entities.FindFirst<PlayerDeadBody>();

            // Kill all other players with "fake deaths" if we are the first player to die
            if (deadPlayers == 0)
            {
                if (!SaveData.Instance.Assists.Invincible || evenIfInvincible)
                {
                    foreach (Player player in players)
                    {
                        if (player != self)
                        {
                            foreach (Follower follower in player.Leader.Followers)
                            {
                                self.Leader.Followers.Add(follower);
                            }

                            if (!player.Dead)
                            {
                                player.Scene.Add(new CustomPlayerDeadBody(player, Vector2.Zero, false, true));
                            }

                            player.Scene.Remove(player);
                        }
                    }

                    return orig(self, direction, evenIfInvincible, registerDeathsInStats);
                }
            }

            return null;
        }

        private static void Player_Added(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
        {
            orig(self, scene);

            List<Player> clones = scene.Tracker.GetEntities<CloneSpawner>().Select(c => (c as CloneSpawner).Clone).ToList<Player>();
            List<Entity> players = scene.Tracker.GetEntities<Player>();

            // Start own respawn animation if there is one player respawning
            foreach (Player player in players)
            {
                if (!clones.Contains(player))
                {
                    Tween respawnTween = (Tween)playerRespawnTween.GetValue(player);

                    if (respawnTween != null && player != self)
                    {
                        self.StateMachine.State = player.StateMachine.State;
                        syncedRespawn.Add(self, player);
                    }
                }
            }
        }

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            // Reset consumed if it was consumed in a different player
            foreach (KeyValuePair<VirtualButton, bool> entry in ConsumedPresses)
            {
                if (entry.Value)
                {
                    virtualButtonConsumed.SetValue(entry.Key, false);
                }
            }

            foreach (KeyValuePair<VirtualButton, float> entry in BufferCounters)
            {
                virtualButtonBufferCounter.SetValue(entry.Key, entry.Value);
            }

            orig(self);

            // Make sure we aren't a frame behind on the synced respawn animation
            if (syncedRespawn.TryGetValue(self, out Player synced))
            {
                if (playerRespawnTween.GetValue(synced) == null)
                {
                    self.StateMachine.State = synced.StateMachine.State;
                    self.Facing = synced.Facing;
                    syncedRespawn.Remove(self);
                }
            }
        }

        private static void setPlayerState(List<Entity> players, Entity ignore, int state)
        {
            foreach (Player player in players)
            {
                if (player != ignore)
                {
                    player.StateMachine.State = state;
                }
            }
        }

        private static IEnumerator Lookout_LookRoutine(On.Celeste.Lookout.orig_LookRoutine orig, Lookout self, Player player)
        {
            List<Entity> players = self.SceneAs<Level>().Tracker.GetEntities<Player>();
            setPlayerState(players, closestLookoutPlayer, Player.StDummy);

            IEnumerator origEnumerator = orig(self, closestLookoutPlayer);
            while (origEnumerator.MoveNext())
            {
                yield return origEnumerator.Current;
            }

            setPlayerState(players, closestLookoutPlayer, Player.StNormal);
        }

        private static void Lookout_StopInteracting(On.Celeste.Lookout.orig_StopInteracting orig, Lookout self)
        {
            orig(self);

            foreach (Player player in self.SceneAs<Level>().Tracker.GetEntities<Player>())
            {
                player.StateMachine.State = Player.StNormal;
            }
        }

        private static void Lookout_Interact(On.Celeste.Lookout.orig_Interact orig, Lookout self, Player player)
        {
            orig(self, player);

            List<Entity> players = self.Scene.Tracker.GetEntities<Player>();
            closestLookoutPlayer = EntityHelper.GetClosestEntity(self, players) as Player;

            setPlayerState(players, closestLookoutPlayer, Player.StDummy);
        }

        private static void TalkComponent_Update(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(1)))
            {
                Logger.Log($"{PandorasBoxModule.LoggerTag}/TalkComponent", $"Patching talk component at {cursor.Index} in CIL code for {cursor.Method.FullName}");

                // this
                cursor.Emit(OpCodes.Ldarg, 0);

                // flag, the current value we are overriding
                cursor.Emit(OpCodes.Ldloc, 1);

                // this.disableDelay
                cursor.Emit(OpCodes.Ldarg, 0);
                cursor.Emit(OpCodes.Ldfld, typeof(TalkComponent).GetField("disableDelay", BindingFlags.Instance | BindingFlags.NonPublic));

                cursor.EmitDelegate<Func<Object, bool, float, bool>>((talkerObject, current, disableDelay) =>
                {
                    TalkComponent talker = talkerObject as TalkComponent;
                    List<Entity> players = talker.Scene.Tracker.GetEntities<Player>();

                    if (players.Count == 1)
                    {
                        return current;
                    }

                    // Check based on vanilla version, except across all player entities

                    if (disableDelay >= 0.05f)
                    {
                        return false;
                    }

                    foreach (Player player in players)
                    {
                        if (player.CollideRect(new Rectangle((int)(talker.Entity.X + talker.Bounds.X), (int)(talker.Entity.Y + talker.Bounds.Y), talker.Bounds.Width, talker.Bounds.Height)) &&
                            player.OnGround() &&
                            player.Bottom < talker.Entity.Y + talker.Bounds.Bottom + 4f &&
                            player.StateMachine.State == 0 &&
                            (!talker.PlayerMustBeFacing || Math.Abs(player.X - talker.Entity.X) <= talker.Bounds.Width || player.Facing == (Facings)Math.Sign(talker.Entity.X - player.X)) &&
                            (TalkComponent.PlayerOver == null || TalkComponent.PlayerOver == talker))
                        {
                            return true;
                        }
                    }

                    return false;
                });

                cursor.Emit(OpCodes.Stloc, 1);
            }
        }
    }
}