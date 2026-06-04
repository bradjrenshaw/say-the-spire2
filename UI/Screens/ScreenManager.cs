using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using SayTheSpire2.Input;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public static class ScreenManager
{
    private static readonly List<Screen> _screenStack = new();
    private static readonly Dictionary<Type, Func<GameScreen>> _gameScreenFactories = new();
    private static IScreenContext? _lastScreenContext;
    private static GameScreen? _lastFactoryScreen;
    private static bool _announceQueued;
    private static bool _announced;
    private static bool _updateAnnounced;

    public static Screen? CurrentScreen =>
        _screenStack.Count > 0 ? _screenStack[^1].DeepestActiveScreen() : null;

    public static void Initialize()
    {
        PushScreen(new DefaultScreen());
        Log.Info("[AccessibilityMod] ScreenManager initialized.");
    }

    /// <summary>
    /// Called each frame from the _Process postfix. Queues the mod version
    /// announcement when the logo or main menu first appears, then keeps
    /// watching for the async update check (<see cref="Updates.UpdateChecker"/>)
    /// to land — appending an "update available" message whenever the
    /// background HTTP request resolves to a newer remote version. Both flags
    /// are reset only by reloading the mod (i.e. once per game launch), so
    /// each session announces at most once.
    /// </summary>
    public static void CheckStartupAnnouncement(Node sceneNode)
    {
        if (_announced && _updateAnnounced) return;

        var currentContext = ActiveScreenContext.Instance.GetCurrentScreen();
        if (currentContext == null) return;

        if (!_announceQueued && currentContext.GetType().Name == "NLogoAnimation")
        {
            _announceQueued = true;
            sceneNode.GetTree().CreateTimer(0.2).Timeout += () =>
            {
                _announced = true;
                Speech.SpeechManager.Output(
                    Localization.Message.Localized("ui", "MOD.VERSION_ANNOUNCE", new { version = ModEntry.Version }));
            };
        }
        else if (!_announceQueued && currentContext.GetType().Name == "NMainMenu")
        {
            // Logo was skipped — announce immediately. Set _announceQueued
            // alongside _announced so this branch can't re-fire every frame:
            // the early-return guard above only kicks in once _updateAnnounced
            // is also set, which never happens on the latest version (or with
            // update checks disabled). Without this flag the version line
            // would loop forever on the main menu.
            _announceQueued = true;
            _announced = true;
            Speech.SpeechManager.Output(
                Localization.Message.Localized("ui", "MOD.VERSION_ANNOUNCE", new { version = ModEntry.Version }));
        }

        // Once the version announce is out and the background update check
        // has reported a newer version, queue a follow-up. The HTTP request
        // can resolve before, during, or after the version announcement —
        // we just wait for both and emit each once.
        if (_announced && !_updateAnnounced)
        {
            var remote = Updates.UpdateChecker.LatestRemoteVersion;
            if (remote != null)
            {
                _updateAnnounced = true;
                Speech.SpeechManager.Output(
                    Localization.Message.Localized("ui", "MOD.UPDATE_AVAILABLE", new { version = remote }));
            }
        }
    }

    /// <summary>
    /// Called each frame to let screens check for state changes.
    /// </summary>
    public static void UpdateAll()
    {
        for (int i = 0; i < _screenStack.Count; i++)
            UpdateRecursive(_screenStack[i]);
    }

    private static void UpdateRecursive(Screen screen)
    {
        screen.OnUpdate();
        if (screen.ActiveChild != null)
            UpdateRecursive(screen.ActiveChild);
    }

    public static void RegisterGameScreen<TGameContext>(Func<GameScreen> factory)
        where TGameContext : class
    {
        _gameScreenFactories[typeof(TGameContext)] = factory;
    }

    public static void PushScreen(Screen screen)
    {
        if (_screenStack.Count > 0)
            _screenStack[^1].OnUnfocus();

        _screenStack.Add(screen);
        screen.OnPush();
        Log.Info($"[AccessibilityMod] Screen pushed: {screen.GetType().Name} (stack depth: {_screenStack.Count})");
    }

    /// <summary>
    /// Remove a specific screen from anywhere in the stack.
    /// Children are recursively popped first.
    /// </summary>
    public static void RemoveScreen(Screen screen)
    {
        var i = _screenStack.IndexOf(screen);
        if (i < 0)
        {
            Log.Error($"[AccessibilityMod] RemoveScreen: {screen.GetType().Name} not found in stack");
            return;
        }

        if (i == 0 && _screenStack.Count == 1)
        {
            Log.Error("[AccessibilityMod] Cannot remove the last Screen!");
            return;
        }

        bool wasTop = i == _screenStack.Count - 1;

        if (wasTop)
            screen.OnUnfocus();

        // Pop children recursively before removing from stack
        if (screen.ActiveChild != null)
            screen.RemoveChild(screen.ActiveChild);

        _screenStack.RemoveAt(i);
        screen.OnPop();

        if (wasTop && _screenStack.Count > 0)
            _screenStack[^1].OnFocus();

        Log.Info($"[AccessibilityMod] Screen removed: {screen.GetType().Name} from index {i} (stack depth: {_screenStack.Count})");
    }

    /// <summary>
    /// Remove a screen from either the tree (if it has a parent) or the flat stack.
    /// </summary>
    public static void RemoveFromTree(Screen screen)
    {
        if (screen.Parent != null)
            screen.Parent.RemoveChild(screen);
        else
            RemoveScreen(screen);
    }

    /// <summary>
    /// Replace a specific screen instance in the stack, preserving its position.
    /// </summary>
    public static void ReplaceScreen(Screen old, Screen replacement)
    {
        var i = _screenStack.IndexOf(old);
        if (i < 0)
        {
            Log.Error($"[AccessibilityMod] ReplaceScreen: {old.GetType().Name} not found in stack");
            return;
        }

        bool wasTop = i == _screenStack.Count - 1;

        if (wasTop)
            old.OnUnfocus();

        if (old.ActiveChild != null)
            old.RemoveChild(old.ActiveChild);
        old.OnPop();

        _screenStack[i] = replacement;
        replacement.OnPush();
        if (wasTop)
            replacement.OnFocus();

        Log.Info($"[AccessibilityMod] Screen replaced: {old.GetType().Name} -> {replacement.GetType().Name} at index {i}");
    }

    /// <summary>
    /// Walk all screens from deepest (innermost child) to shallowest (bottom of stack).
    /// Matches the priority order used by action dispatch.
    /// </summary>
    public static IEnumerable<Screen> WalkScreensDeepestFirst()
    {
        for (int i = _screenStack.Count - 1; i >= 0; i--)
        {
            foreach (var screen in WalkTreeDeepestFirst(_screenStack[i]))
                yield return screen;
        }
    }

    private static IEnumerable<Screen> WalkTreeDeepestFirst(Screen screen)
    {
        if (screen.ActiveChild != null)
        {
            foreach (var child in WalkTreeDeepestFirst(screen.ActiveChild))
                yield return child;
        }
        yield return screen;
    }

    /// <summary>
    /// Dispatch an action through the screen stack. Returns true if consumed.
    /// </summary>
    public static bool DispatchAction(InputAction action, InputActionState state)
    {
        Func<Screen, InputAction, bool> handler = state switch
        {
            InputActionState.JustPressed => (s, a) => s.OnActionJustPressed(a),
            InputActionState.Pressed => (s, a) => s.OnActionPressed(a),
            InputActionState.JustReleased => (s, a) => s.OnActionJustReleased(a),
            _ => (_, _) => false
        };
        return Dispatch(action, handler);
    }

    /// <summary>
    /// Collect all always-enabled buffer keys from the entire screen tree.
    /// </summary>
    public static HashSet<string> GetAlwaysEnabledBuffers()
    {
        var result = new HashSet<string>();
        foreach (var screen in _screenStack)
            CollectBuffers(screen, result);
        return result;
    }

    private static void CollectBuffers(Screen screen, HashSet<string> result)
    {
        foreach (var key in screen.AlwaysEnabledBuffers)
            result.Add(key);
        if (screen.ActiveChild != null)
            CollectBuffers(screen.ActiveChild, result);
    }

    /// <summary>
    /// Resolve a UI element for a control by walking the screen tree deepest-first.
    /// </summary>
    public static UIElement? ResolveElement(Godot.Control control)
    {
        for (int i = _screenStack.Count - 1; i >= 0; i--)
        {
            var result = ResolveElementInTree(_screenStack[i], control);
            if (result != null)
                return result;
        }
        return null;
    }

    private static UIElement? ResolveElementInTree(Screen screen, Godot.Control control)
    {
        if (screen.ActiveChild != null)
        {
            var result = ResolveElementInTree(screen.ActiveChild, control);
            if (result != null)
                return result;
        }
        return screen.GetElement(control);
    }

    /// <summary>
    /// Called when the game's active screen context changes.
    /// Manages pushing/popping GameScreens based on registered factories.
    /// </summary>
    public static void OnGameScreenChanged()
    {
        var currentContext = ActiveScreenContext.Instance.GetCurrentScreen();

        if (currentContext == _lastScreenContext && currentContext != null)
            return;

        _lastScreenContext = currentContext;

        if (currentContext == null)
        {
            // Context cleared — remove the factory-pushed screen
            RemoveActiveGameScreen();
            return;
        }

        // Find a factory for the new context
        Func<GameScreen>? matchedFactory = null;
        var contextType = currentContext.GetType();
        foreach (var (registeredType, factory) in _gameScreenFactories)
        {
            if (registeredType.IsAssignableFrom(contextType))
            {
                matchedFactory = factory;
                break;
            }
        }

        if (matchedFactory != null)
        {
            // Remove the previous factory-pushed screen
            RemoveActiveGameScreen();

            var screen = matchedFactory();
            _lastFactoryScreen = screen;
            PushScreen(screen);
            if (screen.ScreenName is { IsEmpty: false } screenName)
                Speech.SpeechManager.Output(screenName);
        }
        // If no factory found, leave the current screen stack alone —
        // the screen may be manually managed (e.g., settings via ScreenHooks).
    }

    private static void RemoveActiveGameScreen()
    {
        if (_lastFactoryScreen != null && _screenStack.Contains(_lastFactoryScreen))
        {
            RemoveScreen(_lastFactoryScreen);
        }
        _lastFactoryScreen = null;
    }

    private static bool Dispatch(InputAction action, Func<Screen, InputAction, bool> handler)
    {
        for (int i = _screenStack.Count - 1; i >= 0; i--)
        {
            var result = DispatchInTree(_screenStack[i], action, handler);
            if (result.claimed && !result.propagate)
                return true;
        }
        return false;
    }

    private static (bool claimed, bool propagate) DispatchInTree(
        Screen screen, InputAction action, Func<Screen, InputAction, bool> handler)
    {
        // Deepest child first
        if (screen.ActiveChild != null)
        {
            var childResult = DispatchInTree(screen.ActiveChild, action, handler);
            if (childResult.claimed && !childResult.propagate)
                return childResult;
        }

        if (screen.HasClaimed(action.Key))
        {
            // Handler return is "did I consume this?" — true suppresses
            // propagation even when the claim defaults to propagate. Lets
            // a screen conditionally consume (e.g. CombatScreen claiming
            // ui_select with propagate:true so Enter-on-hand-card still
            // reaches the game, but TryConfirmActiveCardPlay returning
            // true while a card-play is active so the same Enter doesn't
            // fall through and trigger the next card).
            bool handlerConsumed = handler(screen, action);
            bool propagate = !handlerConsumed && screen.ShouldPropagate(action.Key);
            return (true, propagate);
        }
        return (false, false);
    }
}
