using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("dialogue", "Dialogue", category: "Other", defaultBuffer: false)]
public class DialogueEvent : GameEvent
{
    private readonly string? _speaker;
    private readonly string _text;

    public DialogueEvent(string? speaker, string text)
    {
        _speaker = speaker;
        _text = text;
    }

    public override Message? GetMessage()
    {
        if (string.IsNullOrEmpty(_text)) return null;
        if (string.IsNullOrEmpty(_speaker)) return Message.Raw(_text);
        return Message.Localized("ui", "DIALOGUE.FORMAT", new { speaker = _speaker, text = _text });
    }
}
