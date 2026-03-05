using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Sts2AccessibilityMod.UI;

public abstract class GameScreen
{
    private readonly Dictionary<Control, UIElement> _registry = new();

    public abstract string ScreenName { get; }

    public void OnOpen()
    {
        _registry.Clear();
        BuildRegistry();
        Log.Info($"[AccessibilityMod] Screen opened: {ScreenName} ({_registry.Count} controls registered)");
    }

    public void OnClose()
    {
        _registry.Clear();
    }

    public virtual void OnUpdate() { }

    public UIElement? GetElement(Control control)
    {
        return _registry.TryGetValue(control, out var element) ? element : null;
    }

    protected void Register(Control control, UIElement element)
    {
        _registry[control] = element;
    }

    protected abstract void BuildRegistry();
}
