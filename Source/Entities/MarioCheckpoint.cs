using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using static Celeste.SummitCheckpoint;

namespace Celeste.Mod.PandorasBox
{
    [Tracked]
    [CustomEntity("pandorasBox/checkpoint")]
    class MarioCheckpoint : Actor
    {
        private bool spawnConfetti;
        private string theme;
        private string activationSound;

        private Sprite sprite;

        public bool Activated;

        public Collider ActivationCollider;

        public MarioCheckpoint(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            spawnConfetti = data.Bool("spawnConfetti", true);
            theme = data.Attr("theme", "flag");
            activationSound = data.Attr("activationSound", "event:/game/07_summit/checkpoint_confetti");
            AllowPushing = data.Bool("moveable", true);

            float width = data.Float("activationWidth", 16f);
            float height = data.Float("activationHeight", 16f);

            Collider = new Hitbox(width, height, -(width / 2), -height);

            Add(sprite = new Sprite(GFX.Game, $"objects/pandorasBox/checkpoint/{theme}/"));

            sprite.AddLoop("active_idle", "active_idle", 0.1f);
            sprite.AddLoop("inactive_idle", "inactive_idle", 0.1f);
            sprite.Add("activating", "activating", 0.1f, "active_idle");
            sprite.Add("deactivating", "deactivating", 0.1f, "inactive_idle");

            sprite.JustifyOrigin(new Vector2(0.5f, 1f));
            sprite.Play("inactive_idle");

            base.Depth = -8999;
        }

        public void DeactivateCheckpoint()
        {
            if (Activated)
            {
                Activated = false;

                sprite.Play("deactivating");
            }
        }

        public void ActivateCheckpoint(bool fromSpawn=false)
        {
            Level level = base.Scene as Level;

            if (Activated)
            {
                return;
            }

            Activated = true;

            if (level == null)
            {
                return;
            }

            sprite.Play(fromSpawn ? "active_idle" : "activating");

            if (fromSpawn)
            {
                return;
            }

            List<Entity> checkpoints = level.Tracker.GetEntities<MarioCheckpoint>();

            foreach (MarioCheckpoint checkpoint in checkpoints)
            {
                if (checkpoint != this)
                {
                    checkpoint.DeactivateCheckpoint();
                }
            }

            level.Session.RespawnPoint = level.GetSpawnPoint(Position);
            level.Session.UpdateLevelStartDashes();
            level.Session.HitCheckpoint = true;

            if (spawnConfetti)
            {
                Scene.Add(new ConfettiRenderer(Position));
                level.Displacement.AddBurst(TopCenter, 0.5f, 4f, 24f, 0.5f);
            }

            Audio.Play(activationSound, Position);
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            Level level = scene as Level;

            if (level == null) {
                return;
            }

            bool sameRespawn = level.Session.RespawnPoint == level.GetSpawnPoint(Position);

            if (!Activated && (CollideCheck<Player>() || sameRespawn))
            {
                ActivateCheckpoint(true);
            }
        }

        public override void Update()
        {
            base.Update();

            if (!Activated)
            {
                Player player = CollideFirst<Player>();
                if (player != null && player.OnGround() && player.Speed.Y >= 0f)
                {
                    ActivateCheckpoint();
                }
            }
        }
    }
}
