using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public abstract class GameScreen : Screen
{
    private readonly Dictionary<Control, UIElement> _registry = new();
    protected readonly HashSet<ulong> _connectedControls = new();

    public override void OnPush()
    {
        _registry.Clear();
        _connectedControls.Clear();
        BuildRegistry();
        Log.Info($"[AccessibilityMod] Screen opened: {ScreenName} ({_registry.Count} controls registered)");
    }

    public override void OnPop()
    {
        _registry.Clear();
    }

    public override UIElement? GetElement(Control control)
    {
        return _registry.TryGetValue(control, out var element) ? element : null;
    }

    protected void Register(Control control, UIElement element)
    {
        _registry[control] = element;
        element.Control = control;
    }

    protected void ClearRegistry()
    {
        _registry.Clear();
    }

    protected IEnumerable<KeyValuePair<Control, UIElement>> GetRegisteredControls()
    {
        return _registry;
    }

    protected abstract void BuildRegistry();

    // --- Shared utilities for screen subclasses ---

    protected void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;
        control.FocusEntered += () => UIManager.SetFocusedControl(control, element);
    }

    protected static bool IsUsable(Control? control)
    {
        return control != null
            && GodotObject.IsInstanceValid(control)
            && control.Visible;
    }

    protected static bool IsVisible(Control? control)
    {
        return control != null && control.Visible;
    }

    protected static void Activate(NClickableControl? control)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;
        control.EmitSignal(NClickableControl.SignalName.Released, control);
    }

    protected static Message? GetButtonStatus(NClickableControl? control)
    {
        if (control == null || control.IsEnabled)
            return null;
        return Message.Localized("ui", "DAILY_RUN.DISABLED");
    }
}
