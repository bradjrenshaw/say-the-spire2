using System;
using System.IO;
using System.Reflection;
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

        _harmony = new Harmony("bradj.sts2-accessibility-mod");
        _harmony.PatchAll(typeof(ModEntry).Assembly);

        InitializeSettings();
        InitializeSpeech();
        InitializeLocalization();
        InitializeBuffers();
        InputManager.Initialize();
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

    private static void InitializeSettings()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SlayTheSpire2", "mods", "SayTheSpire2");

        // Build events category and register all event types
        var eventsCategory = new Settings.CategorySetting("events", "Events");
        Settings.ModSettings.Root.Add(eventsCategory);
        Settings.EventRegistry.Initialize(eventsCategory);

        Settings.EventRegistry.Register(typeof(BlockEvent));
        Settings.EventRegistry.Register(typeof(CardPileEvent));
        Settings.EventRegistry.Register(typeof(CardStolenEvent));
        Settings.EventRegistry.Register(typeof(DeathEvent));
        Settings.EventRegistry.Register(typeof(DialogueEvent));
        Settings.EventRegistry.Register(typeof(EnemyMoveEvent));
        Settings.EventRegistry.Register(typeof(HpEvent));
        Settings.EventRegistry.Register(typeof(PowerEvent));
        Settings.EventRegistry.Register(typeof(TurnEvent));

        // Load saved values (overrides defaults) and write file if first run
        Settings.ModSettings.Initialize(settingsDir);
    }

    private static void InitializeSpeech()
    {
        Speech.SpeechManager.Initialize();
    }

    private static void InitializeLocalization()
    {
        Localization.LocalizationManager.Initialize();
    }

    private static void InitializeBuffers()
    {
        BufferManager.Instance.Add(new Buffers.Buffer("ui"));
        BufferManager.Instance.Add(new Buffers.Buffer("character"));
        BufferManager.Instance.Add(new Buffers.Buffer("relic"));
        BufferManager.Instance.Add(new Buffers.Buffer("card"));
        BufferManager.Instance.Add(new Buffers.Buffer("upgrade"));
        BufferManager.Instance.Add(new Buffers.Buffer("creature"));
        BufferManager.Instance.Add(new Buffers.Buffer("player"));
        BufferManager.Instance.Add(new Buffers.Buffer("events"));
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
