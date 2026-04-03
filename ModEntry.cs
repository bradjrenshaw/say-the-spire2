using System;
using System.IO;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using SayTheSpire2.Buffers;
using SayTheSpire2.Events;
using SayTheSpire2.Input;
using SayTheSpire2.Patches;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public const string Version = "0.3.0";
    public static bool AccessibilityEnabled => Settings.InstallationConfig.ScreenReader;
    private static Harmony? _harmony;

    public static void Initialize()
    {
        Log.Info("[AccessibilityMod] Initializing...");

        // Register assembly resolver BEFORE any System.Speech types are touched
        var modDir = Path.GetDirectoryName(typeof(ModEntry).Assembly.Location)!;
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            var candidate = Path.Combine(modDir, name.Name + ".dll");
            if (File.Exists(candidate))
            {
                Log.Info($"[AccessibilityMod] Resolving {name.Name} from {candidate}");
                return context.LoadFromAssemblyPath(candidate);
            }
            return null;
        };

        // Read installation config (screen_reader, disable_godot_uia)
        var settingsDir = Path.Combine(
            Godot.OS.GetUserDataDir(), "mods", "SayTheSpire2");
        Directory.CreateDirectory(settingsDir);
        Settings.InstallationConfig.Initialize(settingsDir);

        // Always register the accessibility toggle hotkey (Ctrl+Shift+A),
        // even when inert, so users can enable accessibility without the installer.
        Input.AccessibilityToggleHook.Initialize();

        if (!AccessibilityEnabled)
        {
            Log.Info("[AccessibilityMod] Screen reader disabled. Mod loaded but inert.");
            return;
        }

        _harmony = new Harmony("bradj.SayTheSpire2");
        _harmony.PatchAll(typeof(ModEntry).Assembly);

        InitializeLocalization();
        InitializeSettings();
        InitializeSpeech();
        InitializeBuffers();
        InputManager.Initialize();
        InitializeKeybindingSettings();
        ScreenManager.Initialize();
        RegisterScreens();
        if (Settings.InstallationConfig.DisableGodotUia)
            DisableBuiltinAccessibility.Initialize();
        FocusHooks.Initialize(_harmony);
        InputRebindHooks.Initialize(_harmony);
        KeyboardNavHooks.Initialize(_harmony);
        ModalHooks.Initialize(_harmony);
        ScreenHooks.Initialize(_harmony);
        GameOverHooks.Initialize(_harmony);
        TimelineHooks.Initialize(_harmony);
        SettingsScreenHooks.Initialize(_harmony);
        CardPileHooks.Initialize(_harmony);
        HandSelectHooks.Initialize(_harmony);
        OverlayHooks.Initialize(_harmony);
        LobbyHooks.Initialize(_harmony);
        DailyRunHooks.Initialize(_harmony);
        CustomRunHooks.Initialize(_harmony);
        CompendiumHooks.Initialize(_harmony);
        MapScreenHooks.Initialize(_harmony);
        RestSiteHooks.Initialize(_harmony);
        RunLifecycleHooks.Initialize(_harmony);
        CombatNavigationHooks.Initialize(_harmony);
        EventHooks.Initialize(_harmony);
        VotingHooks.Initialize(_harmony);
        MultiplayerHooks.Initialize(_harmony);
        CombatEventManager.Initialize();

        Log.Info("[AccessibilityMod] Initialized. Custom TTS active.");
    }

    public static void SetAccessibilityEnabled(bool enabled)
    {
        Settings.InstallationConfig.SetScreenReader(enabled);
    }

    private static void InitializeSettings()
    {
        var settingsDir = Path.Combine(
            Godot.OS.GetUserDataDir(), "mods", "SayTheSpire2");

        // Register UI element settings
        Settings.ModSettingsRegistry.Register(typeof(UI.Elements.ProxyCard));
        Settings.ModSettingsRegistry.Register(typeof(UI.Elements.ProxyCreature));

        // Each subsystem registers its own defaults
        Settings.FocusStringSettings.RegisterDefaults();
        Settings.EventRegistry.RegisterDefaults();

        // Advanced settings
        var advancedCategory = new Settings.CategorySetting("advanced", "Advanced");
        Settings.ModSettings.Root.Add(advancedCategory);
        var verboseLogging = new Settings.BoolSetting("verbose_logging", "Verbose Logging", false);
        advancedCategory.Add(verboseLogging);
        Events.EventDispatcher.VerboseLogging = verboseLogging.Value;
        verboseLogging.Changed += v => Events.EventDispatcher.VerboseLogging = v;
        var profiling = new Settings.BoolSetting("profiling", "Performance Profiling", false);
        advancedCategory.Add(profiling);
        Events.EventDispatcher.Profiling = profiling.Value;
        profiling.Changed += v => Events.EventDispatcher.Profiling = v;

        // Map settings
        var mapCategory = new Settings.CategorySetting("map", Ui("MAP.SETTINGS.CATEGORY", "Map"));
        Settings.ModSettings.Root.Add(mapCategory);
        mapCategory.Add(new Settings.BoolSetting("auto_advance", Ui("MAP.SETTINGS.AUTO_ADVANCE", "Automatically Follow Paths until Choice Node"), false));
        mapCategory.Add(new Settings.BoolSetting("auto_advance_backward", Ui("MAP.SETTINGS.AUTO_ADVANCE_BACKWARD", "Automatically Follow Paths Backward until Choice Node"), false));
        mapCategory.Add(new Settings.BoolSetting("verbose_backward", Ui("MAP.SETTINGS.VERBOSE_BACKWARD", "Read Intermediate Nodes on Backward Paths"), true));
        mapCategory.Add(new Settings.BoolSetting("announce_current_on_open", Ui("MAP.SETTINGS.ANNOUNCE_CURRENT_ON_OPEN", "Announce Current Location When Map Opens"), true));

        var poiCategory = new Settings.CategorySetting("points_of_interest", Ui("MAP_POI.SETTINGS.CATEGORY", "Points of Interest"));
        mapCategory.Add(poiCategory);
        poiCategory.Add(new Settings.BoolSetting("elite", Ui("MAP_POI.SETTINGS.ELITE", "Elite"), true));
        poiCategory.Add(new Settings.BoolSetting("shop", Ui("MAP_POI.SETTINGS.SHOP", "Shop"), true));
        poiCategory.Add(new Settings.BoolSetting("treasure", Ui("MAP_POI.SETTINGS.TREASURE", "Treasure"), true));
        poiCategory.Add(new Settings.BoolSetting("rest_site", Ui("MAP_POI.SETTINGS.REST_SITE", "Rest Site"), false));
        poiCategory.Add(new Settings.BoolSetting("unknown", Ui("MAP_POI.SETTINGS.UNKNOWN", "Unknown"), false));
        poiCategory.Add(new Settings.BoolSetting("monster", Ui("MAP_POI.SETTINGS.MONSTER", "Monster"), false));
        poiCategory.Add(new Settings.BoolSetting("boss", Ui("MAP_POI.SETTINGS.BOSS", "Boss"), false));
        poiCategory.Add(new Settings.BoolSetting("ancient", Ui("MAP_POI.SETTINGS.ANCIENT", "Ancient"), false));
        poiCategory.Add(new Settings.BoolSetting("quest_marked", Ui("MAP_POI.SETTINGS.QUEST_MARKED", "Quest Marked"), false));

        // Speech handler settings
        var speechCategory = new Settings.CategorySetting("speech", "Speech");
        Settings.ModSettings.Root.Add(speechCategory);
        Speech.SpeechManager.RegisterSettings(speechCategory);

        // Load saved values (overrides defaults) and write file if first run
        Settings.ModSettings.Initialize(settingsDir);
    }

    private static void InitializeKeybindingSettings()
    {
        var keybindingsCategory = new Settings.CategorySetting("keybindings", "Keybindings");
        Settings.ModSettings.Root.Add(keybindingsCategory);

        foreach (var action in InputManager.Actions)
        {
            var bindingSetting = new Settings.BindingSetting(action);
            keybindingsCategory.Add(bindingSetting);
        }

        // Re-load to pick up any saved keybinding overrides
        Settings.ModSettings.Load();
    }

    private static void InitializeSpeech()
    {
        Speech.SpeechManager.Initialize();
    }

    private static void InitializeLocalization()
    {
        var language = "eng";
        try
        {
            var gameLang = MegaCrit.Sts2.Core.Localization.LocManager.Instance?.Language;
            if (!string.IsNullOrEmpty(gameLang))
                language = gameLang;
        }
        catch { }
        Localization.LocalizationManager.Initialize(language);
        Localization.Message.LocalizationResolver = Localization.LocalizationManager.Get;

        // Hook language changes so localization updates when the user switches language in settings
        if (_harmony != null)
        {
            var setLang = HarmonyLib.AccessTools.Method(
                typeof(MegaCrit.Sts2.Core.Localization.LocManager), "SetLanguage");
            if (setLang != null)
            {
                _harmony.Patch(setLang,
                    postfix: new HarmonyLib.HarmonyMethod(typeof(ModEntry), nameof(OnGameLanguageChanged)));
                MegaCrit.Sts2.Core.Logging.Log.Info("[AccessibilityMod] LocManager.SetLanguage hook patched.");
            }
        }
    }

    public static void OnGameLanguageChanged(string language)
    {
        Localization.LocalizationManager.SetLanguage(language);
    }

    private static void InitializeBuffers()
    {
        BufferManager.Instance.RegisterDefaults();
    }

    private static string Ui(string key, string fallback)
    {
        return Localization.LocalizationManager.GetOrDefault("ui", key, fallback);
    }

    private static void RegisterScreens()
    {
        var getContext = () => MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext.ActiveScreenContext.Instance.GetCurrentScreen();

        // Settings screen is managed via OnSubmenuOpened/Closed patches in ScreenHooks
        // instead of RegisterGameScreen, since ActiveScreenContext doesn't detect
        // settings opened from the pause menu during a run.

        ScreenManager.RegisterGameScreen<NMainMenu>(
            () => new MainMenuScreen());

        ScreenManager.RegisterGameScreen<NGameOverScreen>(
            () => new GameOverScreen());

        ScreenManager.RegisterGameScreen<NTimelineScreen>(
            () => new TimelineGameScreen((NTimelineScreen)getContext()!));

        // EpochInspectScreen is managed directly by ScreenHooks (Open/Close patches)
    }
}
