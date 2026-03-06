using System.Collections.Generic;
using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public abstract class Screen
{
    private readonly Dictionary<string, bool> _claimedActions = new();

    public virtual string? ScreenName => null;

    // Input handling — only called for claimed actions
    public virtual bool OnActionJustPressed(InputAction action) => false;
    public virtual bool OnActionPressed(InputAction action) => false;
    public virtual bool OnActionJustReleased(InputAction action) => false;

    // Lifecycle
    public virtual void OnPush() { }
    public virtual void OnPop() { }
    public virtual void OnFocus() { }
    public virtual void OnUnfocus() { }

    // Element registry — screens can optionally map controls to UI elements
    public virtual UIElement? GetElement(Control control) => null;

    /// <summary>
    /// Claim an action for this screen. When propagate is false (default),
    /// the action stops here and lower screens won't see it.
    /// When propagate is true, lower screens also get a chance to handle it.
    /// </summary>
    protected void ClaimAction(string actionKey, bool propagate = false)
    {
        _claimedActions[actionKey] = propagate;
    }

    /// <summary>
    /// Returns true if this screen has claimed the action.
    /// </summary>
    public bool HasClaimed(string actionKey) => _claimedActions.ContainsKey(actionKey);

    /// <summary>
    /// Returns true if a claimed action should propagate to lower screens.
    /// </summary>
    public bool ShouldPropagate(string actionKey) =>
        _claimedActions.TryGetValue(actionKey, out var propagate) && propagate;
}
