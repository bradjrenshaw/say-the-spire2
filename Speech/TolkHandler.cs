using System;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Speech;

public class TolkHandler : ISpeechHandler
{
    private CategorySetting? _settings;
    private BoolSetting? _trySapi;

    public string Key => "tolk";
    public string Label => "Tolk (Screen Reader)";

    public CategorySetting? GetSettings()
    {
        if (_settings != null) return _settings;

        _settings = new CategorySetting(Key, Label);
        _trySapi = new BoolSetting("try_sapi", "Fall back to SAPI", true, localizationKey: "SPEECH.TOLK.TRY_SAPI");
        _settings.Add(_trySapi);

        return _settings;
    }

    public bool Detect()
    {
        try
        {
            return DavyKager.Tolk.IsLoaded() || TryLoad();
        }
        catch
        {
            return false;
        }
    }

    public bool Load()
    {
        try
        {
            return TryLoad();
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] TolkHandler failed to load: {ex}");
            return false;
        }
    }

    public void Unload()
    {
        try
        {
            if (DavyKager.Tolk.IsLoaded())
                DavyKager.Tolk.Unload();
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] TolkHandler failed to unload: {ex}");
        }
    }

    public bool Speak(string text, bool interrupt = false)
    {
        return DavyKager.Tolk.Speak(text, interrupt);
    }

    public bool Output(string text, bool interrupt = false)
    {
        return DavyKager.Tolk.Output(text, interrupt);
    }

    public bool Silence()
    {
        return DavyKager.Tolk.Silence();
    }

    private bool TryLoad()
    {
        DavyKager.Tolk.TrySAPI(_trySapi?.Get() ?? true);
        DavyKager.Tolk.Load();

        if (!DavyKager.Tolk.IsLoaded())
            return false;

        var screenReader = DavyKager.Tolk.DetectScreenReader();
        Log.Info($"[AccessibilityMod] Tolk loaded. Screen reader: {screenReader ?? "none (SAPI fallback)"}");
        return true;
    }
}
