using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Input;

public class InputAction
{
    private readonly string _labelFallback;

    public string Key { get; }
    public string LocalizationKey { get; }
    public string? GameAction { get; }
    private readonly List<InputBinding> _bindings = new();

    /// <summary>
    /// Resolves the localization key if one was supplied at construction,
    /// otherwise returns the raw label. Dynamic so language switches take
    /// effect without restarting the mod.
    /// </summary>
    public string Label => !string.IsNullOrEmpty(LocalizationKey)
        ? LocalizationManager.GetOrDefault("ui", LocalizationKey, _labelFallback)
        : _labelFallback;

    public IReadOnlyList<InputBinding> Bindings => _bindings;

    /// <summary>
    /// Human-readable summary of the action's current bindings (e.g.
    /// "Keyboard: Ctrl+B, Controller: Y"), or a localized "(none)" when
    /// unbound. Single source of truth for showing a binding in the UI —
    /// reused by the keybindings settings rows and the Hotkey Announcements
    /// category labels so they stay in sync.
    /// </summary>
    public string BindingsDisplay =>
        _bindings.Count == 0
            ? LocalizationManager.GetOrDefault("ui", "INPUT.NO_BINDING", "(none)")
            : string.Join(", ", _bindings.Select(b => b.DisplayName));

    public event Action? BindingsChanged;

    public InputAction(string key, string label, string? gameAction = null, string localizationKey = "")
    {
        Key = key;
        _labelFallback = label;
        GameAction = gameAction;
        LocalizationKey = localizationKey;
    }

    public InputAction AddBinding(InputBinding binding)
    {
        _bindings.Add(binding);
        BindingsChanged?.Invoke();
        return this;
    }

    public InputAction AddBinding(Godot.Key keycode, bool ctrl = false, bool shift = false, bool alt = false)
    {
        _bindings.Add(new KeyboardBinding(keycode, ctrl, shift, alt));
        BindingsChanged?.Invoke();
        return this;
    }

    public InputAction AddBinding(ControllerInput input, ControllerInput? modifier = null)
    {
        _bindings.Add(new ControllerBinding(input, modifier));
        BindingsChanged?.Invoke();
        return this;
    }

    public void RemoveBinding(InputBinding binding)
    {
        _bindings.Remove(binding);
        BindingsChanged?.Invoke();
    }

    public void ClearBindings()
    {
        _bindings.Clear();
        BindingsChanged?.Invoke();
    }

    /// <summary>
    /// Check if any keyboard binding matches the given key event.
    /// </summary>
    public bool MatchesKeyEvent(InputEventKey key) => _bindings.OfType<KeyboardBinding>().Any(b => b.Matches(key));

    /// <summary>
    /// Check if any keyboard binding uses the given key (for release detection).
    /// </summary>
    public bool UsesKey(Godot.Key keycode) => _bindings.OfType<KeyboardBinding>().Any(b => b.Keycode == keycode);

    /// <summary>
    /// Check if any controller binding matches the given input, considering held modifiers.
    /// </summary>
    public bool MatchesControllerInput(ControllerInput input, Func<ControllerInput, bool> isHeld)
        => _bindings.OfType<ControllerBinding>().Any(b => b.Matches(input, isHeld));

    /// <summary>
    /// Check if any controller binding uses the given input (primary or modifier) for release detection.
    /// </summary>
    public bool UsesControllerInput(ControllerInput input)
        => _bindings.OfType<ControllerBinding>().Any(b => b.Uses(input));

    /// <summary>
    /// Whether any controller binding requires a modifier.
    /// Used to prioritize modified bindings over unmodified ones.
    /// </summary>
    public bool HasControllerModifier => _bindings.OfType<ControllerBinding>().Any(b => b.Modifier != null);
}
