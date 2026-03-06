using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using Sts2AccessibilityMod.Buffers;
using Sts2AccessibilityMod.Hooks;
using Sts2AccessibilityMod.Events;
using Sts2AccessibilityMod.Patches;
using Sts2AccessibilityMod.UI;
using Sts2AccessibilityMod.UI.Screens;

namespace Sts2AccessibilityMod;

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

        InitializeSpeech();
        InitializeLocalization();
        InitializeBuffers();
        RegisterScreens();
        DisableBuiltinAccessibility.Initialize();
        FocusHooks.Initialize(_harmony);
        InputRebindHooks.Initialize(_harmony);
        KeyboardNavHooks.Initialize(_harmony);
        ModalHooks.Initialize(_harmony);
        ScreenHooks.Initialize(_harmony);
        CombatEventManager.Initialize();

        Log.Info("[AccessibilityMod] Initialized. Custom TTS active.");
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
        BufferManager.Instance.Add(new Buffers.Buffer("creature"));
        BufferManager.Instance.Add(new Buffers.Buffer("player"));
        BufferManager.Instance.Add(new Buffers.Buffer("events"));
    }

    private static void RegisterScreens()
    {
        GameScreenManager.RegisterScreen<NSettingsScreen>(
            () =>
            {
                var context = MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext.ActiveScreenContext.Instance.GetCurrentScreen();
                return new SettingsGameScreen((NSettingsScreen)context!);
            });
    }
}
