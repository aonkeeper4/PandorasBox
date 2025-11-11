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

        public static (bool, Color, Color, Color, Color, List<List<Color>>)? GetVisualSettingsFor(Entity entity)
            => entity.Scene.Tracker.GetEntities<DreamDashController>()
                                   .Cast<DreamDashController>()
                                   .FirstOrDefault(controller => controller.Collider is null || controller.CollideCheck(entity))?
                                   .GetVisualSettings();
    }
}
