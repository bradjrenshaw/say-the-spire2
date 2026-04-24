using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>The element's primary identifying text (creature name, card title, etc.).</summary>
[ShowInGlobalSettings]
public sealed class LabelAnnouncement : Announcement
{
    private readonly Message _label;

    public LabelAnnouncement(string label) : this(Message.Raw(label)) { }
    public LabelAnnouncement(Message label) { _label = label; }

    public override string Key => "label";
    public override Message Render(AnnouncementContext ctx) => _label;
}
