using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>The element's description / tooltip text, typically from the game model.</summary>
[ShowInGlobalSettings]
public sealed class TooltipAnnouncement : Announcement
{
    private readonly Message _text;

    public TooltipAnnouncement(string text) : this(Message.Raw(text)) { }
    public TooltipAnnouncement(Message text) { _text = text; }

    public override string Key => "tooltip";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) => _text;
}
