using System;

namespace Celeste.Mod.PandorasBox;

public class PandorasBoxModule : EverestModule {
    public static PandorasBoxModule Instance { get; private set; }

    public override Type SettingsType => typeof(PandorasBoxModuleuleSettings);
    public static PandorasBoxModuleuleSettings Settings => (PandorasBoxModuleuleSettings) Instance._Settings;

    public override Type SessionType => typeof(PandorasBoxModuleuleSession);
    public static PandorasBoxModuleuleSession Session => (PandorasBoxModuleuleSession) Instance._Session;

    public override Type SaveDataType => typeof(PandorasBoxModuleuleSaveData);
    public static PandorasBoxModuleuleSaveData SaveData => (PandorasBoxModuleuleSaveData) Instance._SaveData;

    public const string LoggerTag = "Pandora's Box";

    public PandorasBoxModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(LoggerTag, LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(LoggerTag, LogLevel.Info);
#endif
    }

    public override void LoadContent(bool firstLoad)
    {
        base.LoadContent(firstLoad);

        FlagToggleSwitch.LoadContent();
    }

    public override void Load()
    {
        CloneSpawner.Load();
        WaterDrowningController.Load();
        TimeField.Load();
        MarioClearPipe.Load();
        DreamDashController.Load();
        ColoredWater.Load();
    }

    public override void Unload()
    {
        CloneSpawner.Unload();
        WaterDrowningController.Unload();
        TimeField.Unload();
        MarioClearPipe.Unload();
        DreamDashController.Unload();
        ColoredWater.Unload();
    }
}