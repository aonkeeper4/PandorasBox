using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.PandorasBox
{
    class PlayerCloneHelper
    {
        public static Dictionary<String, PlayerSpriteMode> modes = new Dictionary<string, PlayerSpriteMode>()
        {
            {"Backpack", PlayerSpriteMode.Madeline},
            {"NoBackpack", PlayerSpriteMode.MadelineNoBackpack},
            {"MadelineAsBadeline", PlayerSpriteMode.MadelineAsBadeline}
        };

        public static Player CreatePlayer(Level level, EntityData data, Vector2 offset)
        {
            string mode = data.Attr("mode");
            Vector2 position = data.Position + offset;

            return CreatePlayer(level, position, mode);
        }

        public static Player CreatePlayer(Level level, Vector2 position, string mode="Inventory")
        {
            Player newPlayer = null;
            List<Player> existingPlayers = (level as Scene).Entities.FindAll<Player>();

            int existingPlayerCount = existingPlayers.Count();
            int facingLeft = existingPlayers.Count((player) => player.Facing == Facings.Left);
            int facingRight = existingPlayerCount - facingLeft;

            if (modes.TryGetValue(mode, out var spriteMode))
            {
                newPlayer = new Player(position, spriteMode);

            } else
            {
                bool hasBackpack = level.Session.Inventory.Backpack;

                newPlayer = new Player(position, hasBackpack ? PlayerSpriteMode.Madeline : PlayerSpriteMode.MadelineNoBackpack);
            }

            // Face the new clone in the direction the majority of existing players are facing
            if (existingPlayerCount > 0)
            {
                newPlayer.Facing = facingRight >= facingLeft ? Facings.Right : Facings.Left;
            }

            return newPlayer;
        }
    }
}
