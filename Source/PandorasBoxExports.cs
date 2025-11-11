using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.PandorasBox;

public static class PandorasBoxExports
{
    internal static void Initialize()
    {
        typeof(DreamDashControllerExports).ModInterop();
    }

    [ModExportName("PandorasBox.DreamDashController")]
    public static class DreamDashControllerExports
    {
        public static void AddSetupIgnoringTypes(List<Type> types)
            => DreamDashController.AddSetupIgnoringTypes(types);
        
        public static void RemoveSetupIgnoringTypes(List<Type> types)
            => DreamDashController.RemoveSetupIgnoringTypes(types);

        public static (Color?, Color?, Color?, Color?, List<List<Color>>) GetVisualSettingsFor(Entity entity)
        {
            DreamDashController controller = entity.Get<DreamDashController.DreamDashControllerComponent>()?.Controller;
            if (controller is not { OverrideColors: true})
                return default;
            
            return (controller.ActiveBackColor,
                controller.DisabledBackColor,
                controller.ActiveLineColor,
                controller.DisabledLineColor,
                controller.ParticleLayerColors);
        }
    }
}
