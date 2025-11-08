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
    [Tracked(true)]
    [CustomEntity("pandorasBox/waterDrowningController")]
    class WaterDrowningController : Entity
    {
        public float WaterDuration;
        public float WaterDrownDuration;
        public string Mode;
        public bool Flashing;

        public WaterDrowningController(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            WaterDuration = 0;
            WaterDrownDuration = data.Float("maxDuration", 10f);
            Mode = data.Attr("mode", "Swimming");
        }

        public override void Update()
        {
            Player player = base.Scene.Tracker.GetEntity<Player>();

            if (player != null)
            {
                bool inWater = PlayerHelper.PlayerInWater(player, Mode);

                if (inWater)
                {
                    WaterDuration += Engine.DeltaTime;
                }
                else
                {
                    WaterDuration = 0f;
                }

                if (inWater && WaterDuration >= WaterDrownDuration && !player.Dead)
                {
                    player.Die(Vector2.Zero);
                }
            }

            float interval = 0f;

            if (WaterDuration > WaterDrownDuration * 0.7)
            {
                interval = 0.6f;
            }
            else if (WaterDuration > WaterDrownDuration * 0.5)
            {
                interval = 1.0f;
            }
            else if (WaterDuration > WaterDrownDuration * 0.3)
            {
                interval = 1.4f;
            }

            if (interval > 0 && base.Scene.OnInterval(interval) && player != null && !player.Dead)
            {
                Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
                Flashing = !Flashing;
            }

            base.Update();
        }

        public static void Load()
        {
            On.Celeste.Player.Render += Player_OnRender;
        }

        public static void Unload()
        {
            On.Celeste.Player.Render -= Player_OnRender;
        }

        private static void Player_OnRender(On.Celeste.Player.orig_Render orig, Player self)
        {
            // Entity is not tracked during code hotswaps, prevents crashes
            WaterDrowningController controller = self.Scene.Tracker.IsEntityTracked<WaterDrowningController>() ? self.Scene.Tracker.GetEntity<WaterDrowningController>() : null;

            if (controller != null && controller.WaterDuration > 0)
            {
                float stamina = self.Stamina;
                self.Stamina = controller.WaterDuration + 2 > controller.WaterDrownDuration ? 0 : Player.ClimbMaxStamina;

                orig(self);

                self.Stamina = stamina;
            }

            else
            {
                orig(self);
            }
        }
    }
}
