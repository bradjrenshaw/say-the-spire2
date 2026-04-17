using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A run's ascension level (e.g., on run-history entries). Caller supplies the
/// pre-formatted ascension value text from the game (typically a short string
/// like "4").
/// </summary>
public sealed class AscensionAnnouncement : Announcement
{
    private readonly string _value;

    public AscensionAnnouncement(string value) { _value = value; }

    public override string Key => "ascension";
    public override string Suffix => ",";
    public override Message Render() =>
        Message.Localized("ui", "RUN_HISTORY.ASCENSION", new { value = _value });
}
