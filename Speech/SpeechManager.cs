using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;

namespace Sts2AccessibilityMod.Speech;

public static class SpeechManager
{
    private static ISpeechHandler? _activeHandler;
    private static bool _initialized;

    private static readonly List<ISpeechHandler> Handlers = new()
    {
        new TolkHandler(),
        new SapiHandler(),
        new ClipboardHandler(),
    };

    public static void Initialize()
    {
        foreach (var handler in Handlers)
        {
            try
            {
                Log.Info($"[AccessibilityMod] Trying speech handler: {handler.Key}");
                if (handler.Detect() && handler.Load())
                {
                    _activeHandler = handler;
                    _initialized = true;
                    Log.Info($"[AccessibilityMod] Active speech handler: {handler.Key}");
                    Output("Accessibility mod loaded.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[AccessibilityMod] Handler {handler.Key} failed: {ex}");
            }
        }

        Log.Error("[AccessibilityMod] No speech handler could be loaded!");
    }

    public static void Speak(string text, bool interrupt = false)
    {
        if (!_initialized || _activeHandler == null) return;
        _activeHandler.Speak(text, interrupt);
    }

    public static void Output(string text, bool interrupt = false)
    {
        if (!_initialized || _activeHandler == null) return;
        _activeHandler.Output(text, interrupt);
    }

    public static void Silence()
    {
        if (!_initialized || _activeHandler == null) return;
        _activeHandler.Silence();
    }
}
