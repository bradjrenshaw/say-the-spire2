using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Input;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public static class ScreenManager
{
    private static readonly List<Screen> _screenStack = new();
    private static readonly Dictionary<Type, Func<GameScreen>> _gameScreenFactories = new();
    private static IScreenContext? _lastScreenContext;

    private static readonly FieldInfo? ListeningEntryField =
        typeof(NInputSettingsPanel).GetField("_listeningEntry", BindingFlags.Instance | BindingFlags.NonPublic);

    public static Screen? CurrentScreen =>
        _screenStack.Count > 0 ? _screenStack[^1] : null;

    public static void Initialize()
    {
        PushScreen(new DefaultScreen());
        Log.Info("[AccessibilityMod] ScreenManager initialized.");
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

    public static void PopScreen()
    {
        if (_screenStack.Count <= 1)
        {
            Log.Error("[AccessibilityMod] Cannot pop the last Screen!");
            return;
        }

        var screen = _screenStack[^1];
        _screenStack.RemoveAt(_screenStack.Count - 1);
        screen.OnPop();

        if (_screenStack.Count > 0)
            _screenStack[^1].OnFocus();

        Log.Info($"[AccessibilityMod] Screen popped: {screen.GetType().Name} (stack depth: {_screenStack.Count})");
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
    /// Try to handle a key event. Returns true if consumed by a mod action.
    /// </summary>
    public static bool HandleKeyEvent(InputEventKey key)
    {
        if (IsGameListeningForInput())
            return false;

        var action = InputManager.MatchAction(key);
        if (action == null)
            return false;

        bool consumed = false;

        if (key.Pressed && !key.Echo)
            consumed = Dispatch(action, (s, a) => s.OnActionJustPressed(a));
        else if (key.Pressed && key.Echo)
            consumed = Dispatch(action, (s, a) => s.OnActionPressed(a));
        else if (!key.Pressed)
            consumed = Dispatch(action, (s, a) => s.OnActionJustReleased(a));

        return consumed;
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

        // Pop existing GameScreen if one is on top
        if (CurrentScreen is GameScreen)
            PopScreen();

        if (currentContext == null)
            return;

        var contextType = currentContext.GetType();
        foreach (var (registeredType, factory) in _gameScreenFactories)
        {
            if (registeredType.IsAssignableFrom(contextType))
            {
                var screen = factory();
                PushScreen(screen);
                if (screen.ScreenName != null)
                    Speech.SpeechManager.Output(screen.ScreenName);
                return;
            }
        }

        Log.Info($"[AccessibilityMod] No GameScreen registered for {contextType.Name}");
    }

    /// <summary>
    /// Check if the game is in a state that needs raw input (e.g., rebinding keys).
    /// </summary>
    private static bool IsGameListeningForInput()
    {
        if (ListeningEntryField == null)
            return false;

        var screen = ActiveScreenContext.Instance.GetCurrentScreen();
        if (screen is not NInputSettingsPanel panel)
            return false;

        return ListeningEntryField.GetValue(panel) != null;
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
