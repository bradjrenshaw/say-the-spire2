using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Input;

namespace SayTheSpire2.Patches;

/// <summary>
/// Patches input hooks:
/// - NControllerManager._Input: intercepts keyboard events
/// - NInputManager._UnhandledKeyInput: suppresses game's keyboard remapping
/// - NInputManager._UnhandledInput: intercepts controller events (they arrive here, not _Input)
/// - NControllerManager._Process: polls hardware for buttons the game doesn't map
/// </summary>
public static class KeyboardNavHooks
{
    public static void Initialize(Harmony harmony)
    {
        try
        {
            PatchSafe(harmony, typeof(NControllerManager), "_Input",
                nameof(InputPrefix), isPrefix: true, "NControllerManager._Input");
            PatchSafe(harmony, typeof(NInputManager), "_UnhandledKeyInput",
                nameof(UnhandledKeyInputPrefix), isPrefix: true, "NInputManager._UnhandledKeyInput");
            PatchSafe(harmony, typeof(NInputManager), "_UnhandledInput",
                nameof(UnhandledInputPrefix), isPrefix: true, "NInputManager._UnhandledInput");
            PatchSafe(harmony, typeof(NControllerManager), "_Process",
                nameof(ProcessPostfix), isPrefix: false, "NControllerManager._Process");
            Log.Info("[AccessibilityMod] Input hooks patched.");
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Input hooks Initialize CRASHED: {e}");
        }
    }

    private static void PatchSafe(Harmony harmony, Type targetType, string methodName,
        string patchMethodName, bool isPrefix, string label)
    {
        try
        {
            var method = targetType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            if (method == null)
            {
                // Try without DeclaredOnly in case it's inherited
                method = AccessTools.Method(targetType, methodName);
            }

            if (method == null)
            {
                Log.Warn($"[AccessibilityMod] {label} NOT found. Available methods:");
                foreach (var m in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    Log.Info($"[AccessibilityMod]   {m.Name}({string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name))})");
                return;
            }

            var patchMethod = new HarmonyMethod(typeof(KeyboardNavHooks).GetMethod(
                patchMethodName, BindingFlags.Static | BindingFlags.Public));

            if (isPrefix)
                harmony.Patch(method, prefix: patchMethod);
            else
                harmony.Patch(method, postfix: patchMethod);

            Log.Info($"[AccessibilityMod] {label} hook patched.");
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] {label} patch FAILED: {e.Message}");
        }
    }

    /// <summary>
    /// Intercept keyboard events on NControllerManager._Input.
    /// </summary>
    public static bool InputPrefix(NControllerManager __instance, InputEvent inputEvent)
    {
        if (InputManager.OnInputEvent(__instance, inputEvent))
        {
            __instance.GetViewport()?.SetInputAsHandled();
            return false;
        }
        return true;
    }

    /// <summary>
    /// Suppress the game's controller-to-action remapping. We handle all controller
    /// input via hardware polling in _Process instead.
    /// </summary>
    public static bool UnhandledInputPrefix()
    {
        return !InputManager.InterceptInput;
    }

    /// <summary>
    /// Poll all controller buttons and axes from hardware.
    /// </summary>
    public static void ProcessPostfix(NControllerManager __instance)
    {
        InputManager.PollCustomActions(__instance);
        UI.Screens.ScreenManager.CheckStartupAnnouncement(__instance);
        UI.Screens.ScreenManager.UpdateAll();
        Events.EventDispatcher.Flush();
    }

    /// <summary>
    /// Suppress the game's own key-to-action remapping when the mod is intercepting input.
    /// </summary>
    public static bool UnhandledKeyInputPrefix()
    {
        return !InputManager.InterceptInput;
    }

}
