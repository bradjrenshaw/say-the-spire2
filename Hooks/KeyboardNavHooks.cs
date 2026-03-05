using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace Sts2AccessibilityMod.Hooks;

/// <summary>
/// Patches NControllerManager so that keyboard navigation keys (arrows, Enter, Escape, etc.)
/// trigger controller/focus mode, enabling keyboard-only menu navigation.
/// </summary>
public static class KeyboardNavHooks
{
    private static readonly PropertyInfo IsUsingControllerProp =
        typeof(NControllerManager).GetProperty("IsUsingController", BindingFlags.Instance | BindingFlags.Public)!;

    private static readonly string[] NavActions = {
        "ui_up", "ui_down", "ui_left", "ui_right",
        "ui_accept", "ui_cancel", "ui_select"
    };

    public static void Initialize(Harmony harmony)
    {
        var target = AccessTools.Method(typeof(NControllerManager), "CheckForControllerInput");
        if (target == null)
        {
            Log.Error("[AccessibilityMod] Could not find CheckForControllerInput!");
            return;
        }

        var prefix = new HarmonyMethod(typeof(KeyboardNavHooks).GetMethod(
            nameof(CheckForControllerInputPrefix), BindingFlags.Static | BindingFlags.Public));

        harmony.Patch(target, prefix: prefix);
        Log.Info("[AccessibilityMod] Keyboard navigation hooks patched.");
    }

    /// <summary>
    /// When in mouse mode, check if a keyboard navigation key was pressed.
    /// If so, switch to controller mode (which enables focus-based navigation).
    /// </summary>
    public static bool CheckForControllerInputPrefix(NControllerManager __instance, InputEvent inputEvent)
    {
        // Check InputEventAction (remapped by NInputManager) and InputEventKey
        bool isNavAction = false;

        if (inputEvent is InputEventAction actionEvent && actionEvent.Pressed)
        {
            foreach (var action in NavActions)
            {
                if (actionEvent.Action == action)
                {
                    isNavAction = true;
                    break;
                }
            }
        }
        else if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Direct keycode fallback for keys that might not go through NInputManager
            isNavAction = keyEvent.Keycode switch
            {
                Key.Up or Key.Down or Key.Left or Key.Right
                or Key.Enter or Key.KpEnter or Key.Escape => true,
                _ => false
            };
        }

        if (!isNavAction)
            return true; // let original run

        // Switch to controller mode
        IsUsingControllerProp.SetValue(__instance, true);

        var viewport = __instance.GetViewport();
        if (viewport != null)
        {
            var mousePos = DisplayServer.MouseGetPosition();
            var windowPos = DisplayServer.WindowGetPosition();
            var localMouse = new Vector2(mousePos.X - windowPos.X, mousePos.Y - windowPos.Y);

            var lastMouseField = AccessTools.Field(typeof(NControllerManager), "_lastMousePosition");
            lastMouseField?.SetValue(__instance, localMouse);

            viewport.WarpMouse(Vector2.One * -1000f);
        }

        ActiveScreenContext.Instance.FocusOnDefaultControl();
        __instance.EmitSignal("ControllerDetected");
        viewport?.SetInputAsHandled();

        Log.Info($"[AccessibilityMod] Keyboard nav: switched to focus mode ({inputEvent.GetType().Name})");

        return false;
    }
}
