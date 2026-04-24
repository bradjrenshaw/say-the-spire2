using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The current status or value of a control (checkbox "checked"/"unchecked",
/// slider's current value, dropdown's selected option, keybinding summary,
/// sort direction, etc.). Takes the pre-formatted text from the caller.
/// </summary>
[ShowInGlobalSettings]
public sealed class StatusAnnouncement : Announcement
{
    private readonly Message _value;

    public StatusAnnouncement(string value) : this(Message.Raw(value)) { }
    public StatusAnnouncement(Message value) { _value = value; }

    public override string Key => "status";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx) => _value;
}
