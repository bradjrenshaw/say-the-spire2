using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using Sts2AccessibilityMod.Speech;

namespace Sts2AccessibilityMod.Hooks;

public static class FocusHooks
{
    private static readonly PropertyInfo IsFocusedProp =
        typeof(NClickableControl).GetProperty("IsFocused", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static void Initialize(Harmony harmony)
    {
        // Manual patching with explicit error reporting
        var refreshFocus = AccessTools.Method(typeof(NClickableControl), "RefreshFocus");
        if (refreshFocus == null)
        {
            Log.Error("[AccessibilityMod] Could not find RefreshFocus method!");

            // List all methods on NClickableControl for debugging
            foreach (var m in typeof(NClickableControl).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                Log.Info($"[AccessibilityMod]   Method: {m.Name} ({m.DeclaringType})");
            }
            return;
        }

        Log.Info($"[AccessibilityMod] Found RefreshFocus: {refreshFocus.DeclaringType}.{refreshFocus.Name}");

        var prefix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPrefix), BindingFlags.Static | BindingFlags.Public));
        var postfix = new HarmonyMethod(typeof(FocusHooks).GetMethod(nameof(RefreshFocusPostfix), BindingFlags.Static | BindingFlags.Public));

        harmony.Patch(refreshFocus, prefix: prefix, postfix: postfix);
        Log.Info("[AccessibilityMod] Focus hooks patched successfully.");
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
            string text = GetAccessibleText(__instance);
            Log.Info($"[AccessibilityMod] Focus: {__instance.Name} -> \"{text}\"");
            if (!string.IsNullOrEmpty(text))
            {
                SpeechManager.Speak(text);
            }
        }
    }

    private static string GetAccessibleText(Control control)
    {
        string? labelText = FindChildText(control);
        if (!string.IsNullOrEmpty(labelText))
            return labelText;
        return CleanNodeName(control.Name);
    }

    private static string? FindChildText(Node node)
    {
        if (node is Label label && !string.IsNullOrWhiteSpace(label.Text))
            return label.Text;
        if (node is RichTextLabel rtl && !string.IsNullOrWhiteSpace(rtl.Text))
            return StripBbcode(rtl.Text);

        foreach (var childName in new[] { "Title", "Label" })
        {
            var child = node.GetNodeOrNull(childName);
            if (child != null)
            {
                var text = FindChildText(child);
                if (text != null) return text;
            }
        }

        for (int i = 0; i < node.GetChildCount(); i++)
        {
            var child = node.GetChild(i);
            var text = FindChildText(child);
            if (text != null) return text;
        }

        return null;
    }

    private static string StripBbcode(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, @"\[.*?\]", "").Trim();
    }

    private static string CleanNodeName(string name)
    {
        return System.Text.RegularExpressions.Regex.Replace(name, @"([a-z])([A-Z])", "$1 $2");
    }
}
