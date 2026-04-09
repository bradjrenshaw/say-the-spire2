using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Input;

/// <summary>
/// Minimal Harmony patch on NControllerManager._Input to listen for Ctrl+Shift+A
/// and toggle accessibility on/off, even when the mod is in inert mode.
/// Uses its own Harmony instance so it works independently of the main mod patches.
/// </summary>
public static class AccessibilityToggleHook
{
    private static bool _triedBootstrapSpeech;
    public static void Initialize()
    {
        try
        {
            var harmony = new Harmony("bradj.SayTheSpire2.AccessibilityToggle");
            var target = typeof(NControllerManager).GetMethod("_Input",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (target == null)
            {
                Log.Error("[AccessibilityMod] Could not find NControllerManager._Input for toggle hook.");
                return;
            }
            var prefix = new HarmonyMethod(typeof(AccessibilityToggleHook), nameof(InputPrefix));
            harmony.Patch(target, prefix: prefix);
            Log.Info("[AccessibilityMod] Accessibility toggle hotkey (Ctrl+Shift+A) registered.");
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Failed to register accessibility toggle: {e.Message}");
        }
    }

    public static bool InputPrefix(NControllerManager __instance, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey key || key.Echo || !key.Pressed)
            return true;

        if (key.Keycode == Key.A && key.CtrlPressed && key.ShiftPressed && !key.AltPressed)
        {
            bool newState = !ModEntry.AccessibilityEnabled;
            ModEntry.SetAccessibilityEnabled(newState);

            string msg = LocalizationManager.GetOrDefault("ui",
                newState ? "SPEECH.ACCESSIBILITY_ENABLED" : "SPEECH.ACCESSIBILITY_DISABLED",
                newState ? "Accessibility enabled. Please restart the game for changes to take effect."
                         : "Accessibility disabled. Please restart the game for changes to take effect.");

            Log.Info($"[AccessibilityMod] {msg}");

            // Try to speak; if speech isn't initialized (inert mode), bootstrap it just for this message
            try
            {
                Speech.SpeechManager.Output(msg);
            }
            catch (System.Exception e) { Log.Info($"[AccessibilityMod] Speech output attempt failed (may not be initialized): {e.Message}"); }

            if (!_triedBootstrapSpeech)
            {
                _triedBootstrapSpeech = true;
                try
                {
                    Speech.SpeechManager.Initialize();
                    Speech.SpeechManager.Output(msg);
                }
                catch (Exception e)
                {
                    Log.Error($"[AccessibilityMod] Could not bootstrap speech for toggle: {e.Message}");
                }
            }

            __instance.GetViewport()?.SetInputAsHandled();
            return false;
        }

        return true;
    }
}
