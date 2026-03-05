using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;
using Sts2AccessibilityMod.Hooks;
using Sts2AccessibilityMod.Patches;

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
        DisableBuiltinAccessibility.Initialize();
        FocusHooks.Initialize(_harmony);
        KeyboardNavHooks.Initialize(_harmony);

        Log.Info("[AccessibilityMod] Initialized. Custom TTS active.");
    }

    private static void InitializeSpeech()
    {
        Speech.SpeechManager.Initialize();
    }
}
