using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.Patches;

public static class InputRebindHooks
{
    private static readonly FieldInfo ListeningEntryField =
        typeof(NInputSettingsPanel).GetField("_listeningEntry", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo KeyboardInputMapField =
        typeof(NInputManager).GetField("_keyboardInputMap", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly FieldInfo ControllerInputMapField =
        typeof(NInputManager).GetField("_controllerInputMap", BindingFlags.Instance | BindingFlags.NonPublic)!;

    // Track state for detecting successful rebinds
    private static NInputSettingsEntry? _previousListeningEntry;
    private static Dictionary<StringName, Key>? _previousKeyboardMap;
    private static Dictionary<StringName, StringName>? _previousControllerMap;

    public static void Initialize(Harmony harmony)
    {
        // Add topPanel to remappable keyboard inputs so it can be rebound
        EnableTopPanelKeyboardRebind();

        // Patch SetAsListeningEntry to announce listening mode
        var setListening = AccessTools.Method(typeof(NInputSettingsPanel), "SetAsListeningEntry");
        if (setListening != null)
        {
            harmony.Patch(setListening,
                postfix: new HarmonyMethod(typeof(InputRebindHooks), nameof(SetAsListeningEntryPostfix)));
            Log.Info("[AccessibilityMod] SetAsListeningEntry hook patched.");
        }

        // Patch _UnhandledKeyInput to announce keyboard rebinds
        var unhandledKey = AccessTools.Method(typeof(NInputSettingsPanel), "_UnhandledKeyInput");
        if (unhandledKey != null)
        {
            harmony.Patch(unhandledKey,
                prefix: new HarmonyMethod(typeof(InputRebindHooks), nameof(KeyInputPrefix)),
                postfix: new HarmonyMethod(typeof(InputRebindHooks), nameof(KeyInputPostfix)));
            Log.Info("[AccessibilityMod] _UnhandledKeyInput hook patched.");
        }

        // Patch _Input to announce controller rebinds
        var inputMethod = AccessTools.Method(typeof(NInputSettingsPanel), "_Input");
        if (inputMethod != null)
        {
            harmony.Patch(inputMethod,
                prefix: new HarmonyMethod(typeof(InputRebindHooks), nameof(ControllerInputPrefix)),
                postfix: new HarmonyMethod(typeof(InputRebindHooks), nameof(ControllerInputPostfix)));
            Log.Info("[AccessibilityMod] NInputSettingsPanel._Input hook patched.");
        }

        // Patch ResetToDefaults to announce reset
        var resetDefaults = AccessTools.Method(typeof(NInputManager), "ResetToDefaults");
        if (resetDefaults != null)
        {
            harmony.Patch(resetDefaults,
                postfix: new HarmonyMethod(typeof(InputRebindHooks), nameof(ResetToDefaultsPostfix)));
            Log.Info("[AccessibilityMod] ResetToDefaults hook patched.");
        }
    }

    public static void SetAsListeningEntryPostfix(NInputSettingsPanel __instance)
    {
        var entry = ListeningEntryField.GetValue(__instance) as NInputSettingsEntry;
        if (entry == null) return;

        var label = GetEntryLabel(entry);
        var text = LocalizationManager.Get("ui", "KEYBIND.LISTENING");
        if (text != null)
            text = text.Replace("{action}", label);
        else
            text = $"Press a key to rebind {label}";

        Log.Info($"[AccessibilityMod] Rebind listening: {label}");
        SpeechManager.Output(text, interrupt: true);
    }

    public static void KeyInputPrefix(NInputSettingsPanel __instance)
    {
        _previousListeningEntry = ListeningEntryField.GetValue(__instance) as NInputSettingsEntry;
        if (_previousListeningEntry != null && NInputManager.Instance != null)
        {
            var map = KeyboardInputMapField.GetValue(NInputManager.Instance) as Dictionary<StringName, Key>;
            _previousKeyboardMap = map != null ? new Dictionary<StringName, Key>(map) : null;
        }
    }

    public static void KeyInputPostfix(NInputSettingsPanel __instance)
    {
        var currentEntry = ListeningEntryField.GetValue(__instance) as NInputSettingsEntry;

        // If we had a listening entry and now it's null, rebind happened
        if (_previousListeningEntry != null && currentEntry == null && NInputManager.Instance != null)
        {
            var inputName = _previousListeningEntry.InputName;
            var label = GetEntryLabel(_previousListeningEntry);
            var newKey = NInputManager.Instance.GetShortcutKey(inputName).ToString();

            // Check if a swap occurred
            string? swapMessage = null;
            if (_previousKeyboardMap != null)
            {
                var currentMap = KeyboardInputMapField.GetValue(NInputManager.Instance) as Dictionary<StringName, Key>;
                if (currentMap != null)
                {
                    foreach (var kvp in currentMap)
                    {
                        if (kvp.Key == inputName) continue;
                        if (_previousKeyboardMap.TryGetValue(kvp.Key, out var oldVal) && oldVal != kvp.Value)
                        {
                            var swappedLabel = GetEntryLabelByInputName(kvp.Key);
                            var swappedKey = kvp.Value.ToString();
                            swapMessage = LocalizationManager.Get("ui", "KEYBIND.SWAPPED");
                            if (swapMessage != null)
                                swapMessage = swapMessage.Replace("{action}", swappedLabel).Replace("{key}", swappedKey);
                            else
                                swapMessage = $"{swappedLabel} swapped to {swappedKey}";
                            break;
                        }
                    }
                }
            }

            var boundText = LocalizationManager.Get("ui", "KEYBIND.BOUND");
            if (boundText != null)
                boundText = boundText.Replace("{action}", label).Replace("{key}", newKey);
            else
                boundText = $"{label} bound to {newKey}";

            if (swapMessage != null)
                boundText = $"{boundText}. {swapMessage}";

            Log.Info($"[AccessibilityMod] Rebind: {boundText}");
            SpeechManager.Output(boundText, interrupt: true);
        }

        _previousListeningEntry = null;
        _previousKeyboardMap = null;
    }

    public static void ControllerInputPrefix(NInputSettingsPanel __instance)
    {
        _previousListeningEntry = ListeningEntryField.GetValue(__instance) as NInputSettingsEntry;
        if (_previousListeningEntry != null && NInputManager.Instance != null)
        {
            var map = ControllerInputMapField.GetValue(NInputManager.Instance) as Dictionary<StringName, StringName>;
            _previousControllerMap = map != null ? new Dictionary<StringName, StringName>(map) : null;
        }
    }

    public static void ControllerInputPostfix(NInputSettingsPanel __instance)
    {
        var currentEntry = ListeningEntryField.GetValue(__instance) as NInputSettingsEntry;

        if (_previousListeningEntry != null && currentEntry == null && NInputManager.Instance != null)
        {
            var inputName = _previousListeningEntry.InputName;
            var label = GetEntryLabel(_previousListeningEntry);

            var map = ControllerInputMapField.GetValue(NInputManager.Instance) as Dictionary<StringName, StringName>;
            var buttonName = "unknown";
            if (map != null && map.TryGetValue(inputName, out var action))
                buttonName = ProxyInputBinding.GetControllerButtonName(action.ToString());

            // Check for swap
            string? swapMessage = null;
            if (_previousControllerMap != null && map != null)
            {
                foreach (var kvp in map)
                {
                    if (kvp.Key == inputName) continue;
                    if (_previousControllerMap.TryGetValue(kvp.Key, out var oldVal) && oldVal != kvp.Value)
                    {
                        var swappedLabel = GetEntryLabelByInputName(kvp.Key);
                        var swappedButton = ProxyInputBinding.GetControllerButtonName(kvp.Value.ToString());
                        swapMessage = LocalizationManager.Get("ui", "KEYBIND.SWAPPED");
                        if (swapMessage != null)
                            swapMessage = swapMessage.Replace("{action}", swappedLabel).Replace("{key}", swappedButton);
                        else
                            swapMessage = $"{swappedLabel} swapped to {swappedButton}";
                        break;
                    }
                }
            }

            var boundText = LocalizationManager.Get("ui", "KEYBIND.BOUND");
            if (boundText != null)
                boundText = boundText.Replace("{action}", label).Replace("{key}", buttonName);
            else
                boundText = $"{label} bound to {buttonName}";

            if (swapMessage != null)
                boundText = $"{boundText}. {swapMessage}";

            Log.Info($"[AccessibilityMod] Controller rebind: {boundText}");
            SpeechManager.Output(boundText, interrupt: true);
        }

        _previousListeningEntry = null;
        _previousControllerMap = null;
    }

    public static void ResetToDefaultsPostfix()
    {
        var text = LocalizationManager.Get("ui", "KEYBIND.RESET") ?? "Bindings reset to defaults";
        Log.Info($"[AccessibilityMod] {text}");
        SpeechManager.Output(text, interrupt: true);
    }

    private static string GetEntryLabel(NInputSettingsEntry entry)
    {
        var labelNode = entry.GetNodeOrNull("%InputLabel");
        if (labelNode != null)
        {
            var text = ProxyElement.FindChildTextPublic(labelNode);
            if (text != null) return text;
        }
        return entry.InputName?.ToString() ?? "unknown";
    }

    private static void EnableTopPanelKeyboardRebind()
    {
        try
        {
            // remappableKeyboardInputs is IReadOnlyList backed by List<StringName>
            if (NInputManager.remappableKeyboardInputs is List<StringName> list)
            {
                var topPanel = MegaCrit.Sts2.Core.ControllerInput.MegaInput.topPanel;
                if (!list.Contains(topPanel))
                {
                    list.Add(topPanel);
                    Log.Info("[AccessibilityMod] Added topPanel to remappable keyboard inputs.");
                }

                // Ensure _keyboardInputMap has an entry for topPanel (default: T)
                if (NInputManager.Instance != null)
                {
                    var map = KeyboardInputMapField.GetValue(NInputManager.Instance) as Dictionary<StringName, Key>;
                    if (map != null && !map.ContainsKey(topPanel))
                    {
                        map[topPanel] = Key.T;
                        Log.Info("[AccessibilityMod] Set default topPanel keyboard binding to T.");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Failed to enable topPanel keyboard rebind: {ex.Message}");
        }
    }

    private static string GetEntryLabelByInputName(StringName inputName)
    {
        // We don't have a reference to the entry, so use the input name as fallback
        // The localized name would require finding the entry node
        return inputName.ToString();
    }
}
