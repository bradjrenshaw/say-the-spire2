using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using Sts2AccessibilityMod.Speech;
using Sts2AccessibilityMod.UI;

namespace Sts2AccessibilityMod.Hooks;

public static class FocusHooks
{
    private static readonly PropertyInfo IsFocusedProp =
        typeof(NClickableControl).GetProperty("IsFocused", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static void Initialize(Harmony harmony)
    {
        // Patch NClickableControl.RefreshFocus for buttons, tickboxes, dropdowns, etc.
        var refreshFocus = AccessTools.Method(typeof(NClickableControl), "RefreshFocus");
        if (refreshFocus == null)
        {
            Log.Error("[AccessibilityMod] Could not find RefreshFocus method!");
            return;
        }

        var prefix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPrefix), BindingFlags.Static | BindingFlags.Public));
        var postfix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPostfix), BindingFlags.Static | BindingFlags.Public));
        harmony.Patch(refreshFocus, prefix: prefix, postfix: postfix);
        Log.Info("[AccessibilityMod] RefreshFocus hook patched.");

        // Patch NSettingsSlider.OnFocus and NPaginator.OnFocus (not NClickableControl subclasses)
        PatchOnFocus<NSettingsSlider>(harmony, nameof(SettingsControlFocusPostfix), "Slider");
        PatchOnFocus<NPaginator>(harmony, nameof(SettingsControlFocusPostfix), "Paginator");
    }

    public static void RefreshFocusPrefix(NClickableControl __instance, out bool __state)
    {
        __state = (bool)IsFocusedProp.GetValue(__instance)!;
    }

    public static void RefreshFocusPostfix(NClickableControl __instance, bool __state)
    {
        bool nowFocused = (bool)IsFocusedProp.GetValue(__instance)!;
        if (nowFocused && !__state)
        {
            AnnounceElement(__instance);
        }
    }

    public static void SettingsControlFocusPostfix(Control __instance)
    {
        AnnounceElement(__instance);
    }

    private static void PatchOnFocus<T>(Harmony harmony, string postfixMethodName, string label)
    {
        var onFocus = AccessTools.Method(typeof(T), "OnFocus");
        if (onFocus != null)
        {
            var postfix = new HarmonyMethod(typeof(FocusHooks).GetMethod(postfixMethodName, BindingFlags.Static | BindingFlags.Public));
            harmony.Patch(onFocus, postfix: postfix);
            Log.Info($"[AccessibilityMod] {label} focus hook patched.");
        }
        else
        {
            Log.Error($"[AccessibilityMod] Could not find {typeof(T).Name}.OnFocus()!");
        }
    }

    private static void AnnounceElement(Control control)
    {
        var element = ResolveElement(control);
        var text = element.GetFocusString();
        Log.Info($"[AccessibilityMod] Focus: {control.GetType().Name} ({control.Name}) -> \"{text}\"");
        if (!string.IsNullOrEmpty(text))
        {
            SpeechManager.Output(text);
        }
    }

    private static UIElement ResolveElement(Control control)
    {
        var screenElement = GameScreenManager.ActiveScreen?.GetElement(control);
        if (screenElement != null)
            return screenElement;

        return ProxyFactory.Create(control);
    }
}
