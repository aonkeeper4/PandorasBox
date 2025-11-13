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
        DreamDashController.SetupIgnoringTypes.Clear();
        DreamDashController.ControlledTypes.Clear();
        
        typeof(DreamDashControllerExports).ModInterop();
    }

    [ModExportName("PandorasBox.DreamDashController")]
    public static class DreamDashControllerExports
    {
        public static void AddSetupIgnoringTypes(List<Type> types)
            => DreamDashController.AddSetupIgnoringTypes(types);
        public static void RemoveSetupIgnoringTypes(List<Type> types)
            => DreamDashController.RemoveSetupIgnoringTypes(types);
        
        public static void AddControlledTypes(List<Type> types)
            => DreamDashController.AddControlledTypes(types);
        public static void RemoveControlledTypes(List<Type> types)
            => DreamDashController.RemoveControlledTypes(types);

        public static void GetGameplaySettingsFor(Entity entity,
            out bool? allowSameDirectionDash,
            out bool? allowDreamDashRedirection,
            out bool? overrideDreamDashSpeed,
            out bool? neverSlowDown,
            out bool? useEntrySpeedAngle,
            out bool? bounceOnCollision,
            out bool? collideStickToWalls,
            out float? sameDirectionSpeedMultiplier,
            out float? dreamDashSpeed)
        {
            allowSameDirectionDash = null;
            allowDreamDashRedirection = null;
            overrideDreamDashSpeed = null;
            neverSlowDown = null;
            useEntrySpeedAngle = null;
            bounceOnCollision = null;
            collideStickToWalls = null;
            sameDirectionSpeedMultiplier = null;
            dreamDashSpeed = null;
            
            if (entity.Get<DreamDashController.DreamDashControllerComponent>()?.Controller is not { } controller)
                return;
            
            allowSameDirectionDash = controller.AllowSameDirectionDash;
            allowDreamDashRedirection = controller.AllowDreamDashRedirection;
            overrideDreamDashSpeed = controller.OverrideDreamDashSpeed;
            neverSlowDown = controller.NeverSlowDown;
            useEntrySpeedAngle = controller.UseEntrySpeedAngle;
            bounceOnCollision = controller.BounceOnCollision;
            collideStickToWalls = controller.CollideStickToWalls;
            sameDirectionSpeedMultiplier = controller.SameDirectionSpeedMultiplier;
            dreamDashSpeed = controller.DreamDashSpeed;
        }
        
        public static void GetVisualSettingsFor(Entity entity,
            out Color? activeBackColor,
            out Color? disabledBackColor,
            out Color? activeLineColor,
            out Color? disabledLineColor,
            out Color[] activeParticleLayerColors,
            out int[] activeParticleLayerIndices,
            out Color[] disabledParticleLayerColors,
            out int[] disabledParticleLayerIndices)
        {
            activeBackColor = null;
            disabledBackColor = null;
            activeLineColor = null;
            disabledLineColor = null;
            activeParticleLayerColors = null;
            activeParticleLayerIndices = null;
            disabledParticleLayerColors = null;
            disabledParticleLayerIndices = null;

            if (entity.Get<DreamDashController.DreamDashControllerComponent>()?.Controller is not { OverrideColors: true } controller)
                return;
            
            Logger.Info(PandorasBoxModule.LoggerTag, $"active particle layer colors: {string.Join(";", controller.ActiveParticleLayerColors.Select(colorList => string.Join(",", colorList)))}");
            Logger.Info(PandorasBoxModule.LoggerTag, $"disabled particle layer colors: {string.Join(";", controller.DisabledParticleLayerColors.Select(colorList => string.Join(",", colorList)))}");
            
            activeBackColor = controller.ActiveBackColor;
            disabledBackColor = controller.DisabledBackColor;
            activeLineColor = controller.ActiveLineColor;
            disabledLineColor = controller.DisabledLineColor;
            (activeParticleLayerColors, activeParticleLayerIndices) = PackArray(controller.ActiveParticleLayerColors);
            (disabledParticleLayerColors, disabledParticleLayerIndices) = PackArray(controller.DisabledParticleLayerColors);
        }
    }
    
    private static (T[], int[]) PackArray<T>(T[][] toPack)
    {
        int[] startingIndices = new int[toPack.Length];
        int sum = 0;
        for (int i = 0; i < toPack.Length; i++)
        {
            startingIndices[i] = sum;
            sum += toPack[i].Length;
        }

        T[] result = new T[sum];
        int index = 0;
        foreach (T[] array in toPack)
        foreach (T element in array)
        {
            result[index] = element;
            index++;
        }

        return (result, startingIndices);
    }
}
