using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public abstract class Screen
{
    private readonly record struct ClaimInfo(bool Propagate, bool FocusedOnly);
    private readonly Dictionary<string, ClaimInfo> _claimedActions = new();
    private bool _claimAll;

    public virtual string? ScreenName => null;

    /// <summary>
    /// Buffer keys that should remain enabled while this screen is in the stack.
    /// Collected from all screens in the stack on each focus change.
    /// </summary>
    public virtual IEnumerable<string> AlwaysEnabledBuffers => System.Array.Empty<string>();

    /// <summary>
    /// Optional root of the container hierarchy for this screen.
    /// When set, focus announcements use path diffing for container context.
    /// </summary>
    public Elements.Container? RootElement { get; protected set; }

    // Child screen tree
    public Screen? Parent { get; private set; }
    public Screen? ActiveChild { get; private set; }

    public void PushChild(Screen child)
    {
        if (ActiveChild != null)
            RemoveChild(ActiveChild);
        child.Parent = this;
        ActiveChild = child;
        child.OnPush();
        Log.Info($"[AccessibilityMod] Screen child pushed: {child.GetType().Name} onto {GetType().Name}");
    }

    public void RemoveChild(Screen child)
    {
        if (child.ActiveChild != null)
            child.RemoveChild(child.ActiveChild);
        child.OnPop();
        child.Parent = null;
        if (ActiveChild == child)
            ActiveChild = null;
        Log.Info($"[AccessibilityMod] Screen child removed: {child.GetType().Name} from {GetType().Name}");
    }

    public Screen DeepestActiveScreen()
    {
        var s = this;
        while (s.ActiveChild != null)
            s = s.ActiveChild;
        return s;
    }

    // Input handling — only called for claimed actions
    public virtual bool OnActionJustPressed(InputAction action) => false;
    public virtual bool OnActionPressed(InputAction action) => false;
    public virtual bool OnActionJustReleased(InputAction action) => false;

    // Lifecycle
    public virtual void OnPush() { }
    public virtual void OnPop() { }
    public virtual void OnFocus() { }
    public virtual void OnUnfocus() { }
    public virtual void OnUpdate() { }

    // Help messages — screens can provide contextual help
    public virtual List<HelpMessage> GetHelpMessages() => new();

    // Element registry — screens can optionally map controls to UI elements
    public virtual UIElement? GetElement(Control control) => null;

    /// <summary>
    /// Claim an action for this screen. When propagate is false (default),
    /// the action stops here and lower screens won't see it.
    /// When propagate is true, lower screens also get a chance to handle it.
    /// When focusedOnly is true, the claim only applies when this screen is
    /// the innermost focused screen (no active child handling input).
    /// </summary>
    protected void ClaimAction(string actionKey, bool propagate = false, bool focusedOnly = false)
    {
        _claimedActions[actionKey] = new ClaimInfo(propagate, focusedOnly);
    }

    /// <summary>
    /// Claim all input actions. Nothing propagates unless explicitly set via ClaimAction.
    /// </summary>
    protected void ClaimAllActions()
    {
        _claimAll = true;
    }

    /// <summary>
    /// Returns true if this screen has claimed the action.
    /// When a claim is focusedOnly, it only applies if this screen is the
    /// current innermost focused screen (top of stack, deepest active child).
    /// </summary>
    public bool HasClaimed(string actionKey)
    {
        if (_claimAll) return true;
        if (!_claimedActions.TryGetValue(actionKey, out var info)) return false;
        if (info.FocusedOnly && ScreenManager.CurrentScreen != this) return false;
        return true;
    }

    /// <summary>
    /// Returns true if a claimed action should propagate to lower screens.
    /// </summary>
    public bool ShouldPropagate(string actionKey) =>
        _claimedActions.TryGetValue(actionKey, out var info) && info.Propagate;
}
