using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Speech;

public static class SpeechManager
{
    private static ISpeechHandler? _activeHandler;
    private static bool _initialized;
    private static ChoiceSetting? _handlerSetting;

    public static readonly IReadOnlyList<ISpeechHandler> Handlers = new List<ISpeechHandler>
    {
        new TolkHandler(),
        new SapiHandler(),
        new ClipboardHandler(),
    };

    public static void RegisterSettings(CategorySetting speechCategory)
    {
        // Handler selection dropdown (auto = try each in order)
        var handlerChoices = new List<Choice>
        {
            new Choice("auto", "Auto"),
        };
        foreach (var handler in Handlers)
            handlerChoices.Add(new Choice(handler.Key, handler.Label));
        var handlerSetting = new ChoiceSetting("handler", "Speech Handler", "auto", handlerChoices, localizationKey: "SPEECH.HANDLER") { SortPriority = -1 };
        speechCategory.Add(handlerSetting);
        SetHandlerSetting(handlerSetting);

        // Per-handler settings
        foreach (var handler in Handlers)
        {
            var handlerSettings = handler.GetSettings();
            if (handlerSettings != null)
                speechCategory.Add(handlerSettings);
        }
    }

    public static void SetHandlerSetting(ChoiceSetting setting)
    {
        _handlerSetting = setting;
        _handlerSetting.Changed += OnHandlerChanged;
    }

    public static void Initialize()
    {
        var preferred = _handlerSetting?.Get() ?? "auto";
        ActivateHandler(preferred);
    }

    public static void Speak(string text, bool interrupt = false)
    {
        if (!_initialized || _activeHandler == null) return;
        _activeHandler.Speak(text, interrupt);
    }

    public static void Speak(Message message, bool interrupt = false)
    {
        Speak(message.Resolve(), interrupt);
    }

    private static readonly System.Diagnostics.Stopwatch _sw = new();

    public static void Output(string text, bool interrupt = false)
    {
        if (!_initialized || _activeHandler == null) return;
        bool profile = Events.EventDispatcher.Profiling;
        if (profile) _sw.Restart();
        _activeHandler.Output(text, interrupt);
        if (profile) { _sw.Stop(); MegaCrit.Sts2.Core.Logging.Log.Info($"[Profile] SpeechManager.Output: {_sw.Elapsed.TotalMilliseconds:F3}ms text=\"{text}\""); }
    }

    public static void Output(Message message, bool interrupt = false)
    {
        Output(message.Resolve(), interrupt);
    }

    public static void Silence()
    {
        if (!_initialized || _activeHandler == null) return;
        _activeHandler.Silence();
    }

    private static void OnHandlerChanged(string key)
    {
        if (!_initialized) return;
        ActivateHandler(key);
        if (_activeHandler != null)
            Output(Message.Localized("ui", "SPEECH.HANDLER_CHANGED", new { handler = _activeHandler.Label }));
    }

    private static void ActivateHandler(string key)
    {
        // Unload current handler
        _activeHandler?.Unload();
        _activeHandler = null;
        _initialized = false;

        if (key == "auto")
        {
            // Try each in order
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
        else
        {
            // Try specific handler
            var handler = Handlers.FirstOrDefault(h => h.Key == key);
            if (handler == null)
            {
                Log.Error($"[AccessibilityMod] Unknown speech handler: {key}");
                return;
            }

            try
            {
                if (handler.Load())
                {
                    _activeHandler = handler;
                    _initialized = true;
                    Log.Info($"[AccessibilityMod] Active speech handler: {handler.Key}");
                }
                else
                {
                    Log.Error($"[AccessibilityMod] Speech handler {key} failed to load, falling back to auto");
                    ActivateHandler("auto");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[AccessibilityMod] Speech handler {key} failed: {ex}, falling back to auto");
                ActivateHandler("auto");
            }
        }
    }
}
