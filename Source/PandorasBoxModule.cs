using System;

namespace Celeste.Mod.PandorasBox;

public class PandorasBoxModule : EverestModule {
    public static PandorasBoxModule Instance { get; private set; }

    public override Type SettingsType => typeof(PandorasBoxModuleSettings);
    public static PandorasBoxModuleSettings Settings => (PandorasBoxModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(PandorasBoxModuleSession);
    public static PandorasBoxModuleSession Session => (PandorasBoxModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(PandorasBoxModuleSaveData);
    public static PandorasBoxModuleSaveData SaveData => (PandorasBoxModuleSaveData) Instance._SaveData;

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
        
        PandorasBoxExports.Initialize();
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