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
    public const string Version = "0.1.4";
    public static bool AccessibilityEnabled { get; private set; }
    private static Harmony? _harmony;
    private static string? _accessibilityPath;

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

        // Check accessibility toggle before doing anything else
        var settingsDir = Path.Combine(
            Godot.OS.GetUserDataDir(), "mods", "SayTheSpire2");
        Directory.CreateDirectory(settingsDir);
        _accessibilityPath = Path.Combine(settingsDir, "accessibility.json");
        AccessibilityEnabled = ReadAccessibilityEnabled();

        // Always register the accessibility toggle hotkey (Ctrl+Shift+A),
        // even when inert, so users can enable accessibility without the installer.
        Input.AccessibilityToggleHook.Initialize();

        if (!AccessibilityEnabled)
        {
            Log.Info("[AccessibilityMod] Accessibility disabled. Mod loaded but inert.");
            return;
        }

        _harmony = new Harmony("bradj.SayTheSpire2");
        _harmony.PatchAll(typeof(ModEntry).Assembly);

        InitializeSettings();
        InitializeSpeech();
        InitializeLocalization();
        InitializeBuffers();
        InputManager.Initialize();
        InitializeKeybindingSettings();
        ScreenManager.Initialize();
        RegisterScreens();
        DisableBuiltinAccessibility.Initialize();
        FocusHooks.Initialize(_harmony);
        InputRebindHooks.Initialize(_harmony);
        KeyboardNavHooks.Initialize(_harmony);
        ModalHooks.Initialize(_harmony);
        ScreenHooks.Initialize(_harmony);
        CombatNavigationHooks.Initialize(_harmony);
        EventHooks.Initialize(_harmony);
        CombatEventManager.Initialize();

        Log.Info("[AccessibilityMod] Initialized. Custom TTS active.");
    }

    private static bool ReadAccessibilityEnabled()
    {
        try
        {
            if (_accessibilityPath == null || !File.Exists(_accessibilityPath))
                return false;

            var json = File.ReadAllText(_accessibilityPath);
            var doc = System.Text.Json.JsonSerializer.Deserialize<
                System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(json);
            if (doc != null && doc.TryGetValue("enabled", out var val) && val.ValueKind == System.Text.Json.JsonValueKind.True)
                return true;
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to read accessibility.json: {e.Message}");
        }
        return false;
    }

    public static void SetAccessibilityEnabled(bool enabled)
    {
        try
        {
            if (_accessibilityPath == null) return;
            var json = System.Text.Json.JsonSerializer.Serialize(
                new { enabled }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_accessibilityPath, json);
            AccessibilityEnabled = enabled;
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to write accessibility.json: {e.Message}");
        }
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
        var mapCategory = new Settings.CategorySetting("map", "Map");
        Settings.ModSettings.Root.Add(mapCategory);
        mapCategory.Add(new Settings.BoolSetting("auto_advance", "Automatically Follow Paths until Choice Node", false));
        mapCategory.Add(new Settings.BoolSetting("auto_advance_backward", "Automatically Follow Paths Backward until Choice Node", false));
        mapCategory.Add(new Settings.BoolSetting("verbose_backward", "Read Intermediate Nodes on Backward Paths", true));
        mapCategory.Add(new Settings.BoolSetting("announce_current_on_open", "Announce Current Location When Map Opens", true));

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
        Localization.LocalizationManager.Initialize();
        Localization.Message.LocalizationResolver = Localization.LocalizationManager.Get;
    }

    private static void InitializeBuffers()
    {
        BufferManager.Instance.RegisterDefaults();
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
