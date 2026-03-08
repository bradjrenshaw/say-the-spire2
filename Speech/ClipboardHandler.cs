using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Speech;

public class ClipboardHandler : ISpeechHandler
{
    public string Key => "clipboard";
    public string Label => "Clipboard";
    public Settings.CategorySetting? GetSettings() => null;

    public bool Detect() => true;

    public bool Load()
    {
        Log.Info("[AccessibilityMod] Clipboard handler loaded (fallback).");
        return true;
    }

    public void Unload() { }

    public bool Speak(string text, bool interrupt = false)
    {
        return Output(text, interrupt);
    }

    public bool Output(string text, bool interrupt = false)
    {
        Godot.DisplayServer.ClipboardSet(text);
        return true;
    }

    public bool Silence() => true;
}
