using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Speech;

public class SapiHandler : ISpeechHandler
{
    private SpeechSynthesizer? _synth;
    private CategorySetting? _settings;
    private IntSetting? _rate;
    private IntSetting? _volume;
    private ChoiceSetting? _voice;

    public string Key => "sapi";
    public string Label => "SAPI";

    public CategorySetting? GetSettings()
    {
        if (_settings != null) return _settings;

        _settings = new CategorySetting(Key, Label);

        _rate = new IntSetting("rate", "Rate", defaultValue: 2, min: -10, max: 10);
        _volume = new IntSetting("volume", "Volume", defaultValue: 100, min: 0, max: 100, step: 5);

        // Enumerate installed voices
        var voices = new List<Choice>();
        try
        {
            using var tempSynth = new SpeechSynthesizer();
            foreach (var v in tempSynth.GetInstalledVoices().Where(v => v.Enabled))
            {
                voices.Add(new Choice(v.VoiceInfo.Name, v.VoiceInfo.Name, v.VoiceInfo));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] Failed to enumerate SAPI voices: {ex}");
        }

        var defaultVoice = voices.FirstOrDefault()?.Key ?? "default";
        _voice = new ChoiceSetting("voice", "Voice", defaultVoice, voices);

        _settings.Add(_rate);
        _settings.Add(_volume);
        _settings.Add(_voice);

        // Subscribe to changes
        _rate.Changed += v => { if (_synth != null) _synth.Rate = v; };
        _volume.Changed += v => { if (_synth != null) _synth.Volume = v; };
        _voice.Changed += v =>
        {
            if (_synth != null)
            {
                try { _synth.SelectVoice(v); }
                catch (Exception ex) { Log.Error($"[AccessibilityMod] Failed to select voice '{v}': {ex}"); }
            }
        };

        return _settings;
    }

    public bool Detect()
    {
        try
        {
            using var synth = new SpeechSynthesizer();
            return true;
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
            _synth = new SpeechSynthesizer();
            _synth.Rate = _rate?.Get() ?? 2;
            _synth.Volume = _volume?.Get() ?? 100;

            var voiceName = _voice?.Get();
            if (!string.IsNullOrEmpty(voiceName) && voiceName != "default")
            {
                try { _synth.SelectVoice(voiceName); }
                catch (Exception ex) { Log.Error($"[AccessibilityMod] Failed to select voice '{voiceName}': {ex}"); }
            }

            Log.Info("[AccessibilityMod] SAPI handler loaded.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] SapiHandler failed to load: {ex}");
            return false;
        }
    }

    public void Unload()
    {
        _synth?.Dispose();
        _synth = null;
    }

    public bool Speak(string text, bool interrupt = false)
    {
        if (_synth == null) return false;
        if (interrupt) _synth.SpeakAsyncCancelAll();
        _synth.SpeakAsync(text);
        return true;
    }

    public bool Output(string text, bool interrupt = false)
    {
        return Speak(text, interrupt);
    }

    public bool Silence()
    {
        if (_synth == null) return false;
        _synth.SpeakAsyncCancelAll();
        return true;
    }
}
