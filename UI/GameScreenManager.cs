using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace Sts2AccessibilityMod.UI;

public static class GameScreenManager
{
    private static readonly Dictionary<Type, Func<GameScreen>> _screenFactories = new();
    private static GameScreen? _activeScreen;
    private static IScreenContext? _lastScreenContext;

    public static GameScreen? ActiveScreen => _activeScreen;

    public static void RegisterScreen<TGameContext>(Func<GameScreen> factory)
        where TGameContext : class
    {
        _screenFactories[typeof(TGameContext)] = factory;
    }

    public static void OnScreenChanged()
    {
        var currentContext = ActiveScreenContext.Instance.GetCurrentScreen();

        // Same screen context, no change needed
        if (currentContext == _lastScreenContext && currentContext != null)
            return;

        _lastScreenContext = currentContext;

        // Close previous screen
        if (_activeScreen != null)
        {
            _activeScreen.OnClose();
            _activeScreen = null;
        }

        if (currentContext == null)
            return;

        // Find a registered GameScreen for this context type
        var contextType = currentContext.GetType();
        foreach (var (registeredType, factory) in _screenFactories)
        {
            if (registeredType.IsAssignableFrom(contextType))
            {
                _activeScreen = factory();
                _activeScreen.OnOpen();
                Speech.SpeechManager.Output(_activeScreen.ScreenName);
                return;
            }
        }

        Log.Info($"[AccessibilityMod] No GameScreen registered for {contextType.Name}");
    }
}
