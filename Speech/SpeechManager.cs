using System;
using System.Speech.Synthesis;
using MegaCrit.Sts2.Core.Logging;

namespace Sts2AccessibilityMod.Speech;

public static class SpeechManager
{
    private static SpeechSynthesizer? _synth;
    private static bool _initialized;

    public static void Initialize()
    {
        try
        {
            _synth = new SpeechSynthesizer();
            _synth.Rate = 2; // slightly faster than default, adjust as needed
            _synth.Volume = 100;

            // List available voices for debugging
            foreach (var voice in _synth.GetInstalledVoices())
            {
                var info = voice.VoiceInfo;
                Log.Info($"[AccessibilityMod] Available voice: {info.Name} ({info.Culture})");
            }

            _initialized = true;
            Speak("Accessibility mod loaded.");
        }
        catch (Exception ex)
        {
            Log.Error($"[AccessibilityMod] Failed to initialize TTS: {ex}");
        }
    }

    /// <summary>
    /// Speak text, interrupting any current speech.
    /// Use this for focus changes, button presses, etc.
    /// </summary>
    public static void Speak(string text)
    {
        if (!_initialized || _synth == null) return;
        _synth.SpeakAsyncCancelAll();
        _synth.SpeakAsync(text);
    }

    /// <summary>
    /// Speak text without interrupting current speech.
    /// Use this for queued announcements like combat log entries.
    /// </summary>
    public static void SpeakQueued(string text)
    {
        if (!_initialized || _synth == null) return;
        _synth.SpeakAsync(text);
    }

    /// <summary>
    /// Stop all speech immediately.
    /// </summary>
    public static void Stop()
    {
        if (!_initialized || _synth == null) return;
        _synth.SpeakAsyncCancelAll();
    }

    public static void SetRate(int rate)
    {
        if (_synth != null) _synth.Rate = Math.Clamp(rate, -10, 10);
    }

    public static void SetVolume(int volume)
    {
        if (_synth != null) _synth.Volume = Math.Clamp(volume, 0, 100);
    }
}
