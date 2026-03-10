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
    private static bool _announceQueued;
    private static bool _announced;

    public static Screen? CurrentScreen =>
        _screenStack.Count > 0 ? _screenStack[^1] : null;

    public static void Initialize()
    {
        PushScreen(new DefaultScreen());
        Log.Info("[AccessibilityMod] ScreenManager initialized.");
    }

    /// <summary>
    /// Called each frame from the _Process postfix. Checks for the logo screen
    /// and queues the mod version announcement with a short delay.
    /// </summary>
    public static void CheckStartupAnnouncement(Node sceneNode)
    {
        if (_announced) return;

        var currentContext = ActiveScreenContext.Instance.GetCurrentScreen();
        if (currentContext == null) return;

        if (!_announceQueued && currentContext.GetType().Name == "NLogoAnimation")
        {
            _announceQueued = true;
            sceneNode.GetTree().CreateTimer(0.2).Timeout += () =>
            {
                _announced = true;
                Speech.SpeechManager.Output(
                    Localization.Message.Raw($"Say the Spire {ModEntry.Version}"));
            };
        }
        else if (!_announceQueued && currentContext.GetType().Name == "NMainMenu")
        {
            // Logo was skipped — announce immediately
            _announced = true;
            Speech.SpeechManager.Output(
                Localization.Message.Raw($"Say the Spire {ModEntry.Version}"));
        }
    }

    /// <summary>
    /// Called each frame to let screens check for state changes.
    /// </summary>
    public static void UpdateAll()
    {
        for (int i = 0; i < _screenStack.Count; i++)
            _screenStack[i].OnUpdate();
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
    /// Safe to call even if other screens have been pushed on top.
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

        _screenStack.RemoveAt(i);
        screen.OnPop();

        if (wasTop && _screenStack.Count > 0)
            _screenStack[^1].OnFocus();

        Log.Info($"[AccessibilityMod] Screen removed: {screen.GetType().Name} from index {i} (stack depth: {_screenStack.Count})");
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
        old.OnPop();

        _screenStack[i] = replacement;
        replacement.OnPush();
        if (wasTop)
            replacement.OnFocus();

        Log.Info($"[AccessibilityMod] Screen replaced: {old.GetType().Name} -> {replacement.GetType().Name} at index {i}");
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
    /// Collect all always-enabled buffer keys from the entire screen stack.
    /// </summary>
    public static HashSet<string> GetAlwaysEnabledBuffers()
    {
        var result = new HashSet<string>();
        foreach (var screen in _screenStack)
        {
            foreach (var key in screen.AlwaysEnabledBuffers)
                result.Add(key);
        }
        return result;
    }

    /// <summary>
    /// Resolve a UI element for a control by walking the screen stack top-down.
    /// </summary>
    public static UIElement? ResolveElement(Godot.Control control)
    {
        for (int i = _screenStack.Count - 1; i >= 0; i--)
        {
            var element = _screenStack[i].GetElement(control);
            if (element != null)
                return element;
        }
        return null;
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
            // Context cleared — remove any active GameScreen
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
            // Remove existing GameScreen before pushing the new one
            RemoveActiveGameScreen();

            var screen = matchedFactory();
            PushScreen(screen);
            if (screen.ScreenName != null)
                Speech.SpeechManager.Output(Localization.Message.Raw(screen.ScreenName));
        }
        // If no factory found, leave the current screen stack alone —
        // the screen may be manually managed (e.g., settings via ScreenHooks).
    }

    private static void RemoveActiveGameScreen()
    {
        for (int i = _screenStack.Count - 1; i >= 0; i--)
        {
            if (_screenStack[i] is GameScreen gs)
            {
                RemoveScreen(gs);
                return;
            }
        }
    }

    private static bool Dispatch(InputAction action, Func<Screen, InputAction, bool> handler)
    {
        for (int i = _screenStack.Count - 1; i >= 0; i--)
        {
            var screen = _screenStack[i];
            if (!screen.HasClaimed(action.Key))
                continue;

            handler(screen, action);

            if (!screen.ShouldPropagate(action.Key))
                return true;
        }
        return false;
    }
}
