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
    public const string Version = "0.4.1";
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
        DevConsoleHooks.Initialize(_harmony);
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

        // Each subsystem registers its own defaults
        Settings.EventRegistry.RegisterDefaults();
        UI.Announcements.AnnouncementRegistry.RegisterDefaults();

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
        var keybindingsCategory = new Settings.CategorySetting("keybindings", Ui("KEYBINDINGS.CATEGORY", "Keybindings"));
        Settings.ModSettings.Root.Add(keybindingsCategory);

        foreach (var action in InputManager.Actions)
        {
            var category = GetOrCreateKeybindingCategory(keybindingsCategory, GetKeybindingCategoryPath(action.Key));
            var bindingSetting = new Settings.BindingSetting(action)
            {
                SortPriority = GetKeybindingSortPriority(action.Key),
            };
            category.Add(bindingSetting);
        }

        // Re-load to pick up any saved keybinding overrides
        Settings.ModSettings.Load();
    }

    private static Settings.CategorySetting GetOrCreateKeybindingCategory(
        Settings.CategorySetting root,
        params (string Key, string Label)[] path)
    {
        var current = root;
        foreach (var (key, label) in path)
        {
            var existing = current.Get<Settings.CategorySetting>(key);
            if (existing != null)
            {
                current = existing;
                continue;
            }

            // These categories are UI-only; keeping them out of the serialized
            // path preserves existing keybind saves while allowing nested menus.
            var category = new Settings.CategorySetting(key, label, includeInPath: false);
            current.Add(category);
            current = category;
        }
        return current;
    }

    private static (string Key, string Label)[] GetKeybindingCategoryPath(string actionKey)
    {
        if (actionKey.StartsWith("mega_select_card_"))
        {
            return new[]
            {
                ("combat", Ui("KEYBINDINGS.CATEGORIES.COMBAT", "Combat")),
                ("combatant_status", Ui("KEYBINDINGS.CATEGORIES.COMBATANT_STATUS", "Combatant Status")),
            };
        }

        if (actionKey.StartsWith("announce_combatant_intent_"))
        {
            return new[]
            {
                ("combat", Ui("KEYBINDINGS.CATEGORIES.COMBAT", "Combat")),
                ("combatant_intent", Ui("KEYBINDINGS.CATEGORIES.COMBATANT_INTENT", "Combatant Intent")),
            };
        }

        return actionKey switch
        {
            "ui_accept" or
            "ui_select" or
            "ui_cancel" or
            "ui_up" or
            "ui_down" or
            "ui_left" or
            "ui_right" or
            "mega_pause_and_back" or
            "mega_peek" => new[]
            {
                ("navigation", Ui("KEYBINDINGS.CATEGORIES.NAVIGATION", "Navigation")),
            },

            "buffer_next_item" or
            "buffer_prev_item" or
            "buffer_next" or
            "buffer_prev" => new[]
            {
                ("buffers", Ui("KEYBINDINGS.CATEGORIES.BUFFERS", "Buffers")),
            },

            "announce_gold" or
            "announce_hp" or
            "announce_boss" or
            "announce_relic_counters" or
            "mega_top_panel" or
            "mega_view_deck_and_tab_left" or
            "mega_view_exhaust_pile_and_tab_right" or
            "mega_view_map" => new[]
            {
                ("run_information", Ui("KEYBINDINGS.CATEGORIES.RUN_INFORMATION", "Run Information")),
            },

            "announce_block" or
            "announce_energy" or
            "announce_powers" or
            "announce_intents" or
            "announce_summarized_intents" or
            "mega_release_card" or
            "mega_view_draw_pile" or
            "mega_view_discard_pile" => new[]
            {
                ("combat", Ui("KEYBINDINGS.CATEGORIES.COMBAT", "Combat")),
                ("general", Ui("KEYBINDINGS.CATEGORIES.GENERAL", "General")),
            },

            "map_poi_prev" or
            "map_poi_next" or
            "map_poi_toggle_mode" => new[]
            {
                ("map", Ui("KEYBINDINGS.CATEGORIES.MAP", "Map")),
                ("points_of_interest", Ui("MAP_POI.SETTINGS.CATEGORY", "Points of Interest")),
            },

            "map_toggle_current_marker" or
            "map_clear_all_markers" => new[]
            {
                ("map", Ui("KEYBINDINGS.CATEGORIES.MAP", "Map")),
                ("markers", Ui("KEYBINDINGS.CATEGORIES.MARKERS", "Markers")),
            },

            "help" or
            "mod_settings" or
            "reset_bindings" => new[]
            {
                ("mod", Ui("KEYBINDINGS.CATEGORIES.MOD", "Mod")),
            },

            _ => new[]
            {
                ("mod", Ui("KEYBINDINGS.CATEGORIES.MOD", "Mod")),
            },
        };
    }

    private static int GetKeybindingSortPriority(string actionKey)
    {
        if (actionKey.StartsWith("mega_select_card_") &&
            int.TryParse(actionKey["mega_select_card_".Length..], out var combatantStatusIndex))
        {
            return combatantStatusIndex;
        }

        if (actionKey.StartsWith("announce_combatant_intent_") &&
            int.TryParse(actionKey["announce_combatant_intent_".Length..], out var combatantIntentIndex))
        {
            return combatantIntentIndex;
        }

        return 0;
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
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Language detection failed: {e.Message}"); }
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
