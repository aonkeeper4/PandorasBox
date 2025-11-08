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
    [CustomEntity("pandorasBox/laserNoteBlock")]
    class LaserNoteblock : LaserDetectorActor
    {
        private int pitch;
        private string sound;
        private string direction;
        private bool atPlayer;
        private float volume;

        private Sprite sprite;

        private Dictionary<string, Collider> directionBlockingColliders = new Dictionary<string, Collider>()
        {
            {
                "Vertical",
                new ColliderList(new Collider[] {
                    new Hitbox(3f, 16f, -8f, -8f),
                    new Hitbox(3f, 16f, 5f, -8f)
                })
            },
            {
                "Horizontal",
                new ColliderList(new Collider[] {
                    new Hitbox(16f, 3f, -8f, -8f),
                    new Hitbox(16f, 3f, -8f, 5f)
                })
            },
        };

        private Dictionary<string, string> textureLookup = new Dictionary<string, string>()
        {
            {"Horizontal", "objects/pandorasBox/laser/noteblock/noteblock_horizontal"},
            {"Vertical", "objects/pandorasBox/laser/noteblock/noteblock_vertical"}
        };

        private Dictionary<string, string> loopName = new Dictionary<string, string>()
        {
            {"Horizontal", "noteblock_horizontal"},
            {"Vertical", "noteblock_vertical"}
        };

        public LaserNoteblock(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            pitch = data.Int("pitch", 69);
            direction = data.Attr("direction", "Horizontal");
            sound = data.Attr("sound", "game_03_deskbell_again");
            volume = data.Float("volume", 1f);
            atPlayer = data.Bool("atPlayer", false);

            Add((Component)(sprite = new Sprite(GFX.Game, textureLookup[direction])));
            sprite.AddLoop(loopName[direction], "", 0.125f);
            sprite.CenterOrigin();

            sprite.Play(loopName[direction]);

            Collider = directionBlockingColliders[direction];
            LaserBlockingCollider = directionBlockingColliders[direction];
            LaserDetectionCollider = new Hitbox(8f, 8f, -4f, -4f);

            Depth = 50;
        }

        public override void OnNewLaserbeam(Laserbeam laserbeams)
        {
            Player player = base.Scene.Tracker.GetEntity<Player>();
            Vector2 position = player != null && atPlayer ? player.Position : Position;

            float pitch = NoteHelper.relativeA4ToFreq(this.pitch - 69) / 440f;

            FMOD.Studio.EventInstance instance = Audio.Play(SFX.EventnameByHandle(sound), position);

            instance?.setVolume(volume);
            instance?.setPitch(pitch);
        }
    }
}
