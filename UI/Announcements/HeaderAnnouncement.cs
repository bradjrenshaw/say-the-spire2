using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context header line: title plus secondary descriptors (type,
/// rarity) joined with commas. Distinct from the focus context's
/// LabelAnnouncement + TypeAnnouncement + SubtypeAnnouncement triple — the
/// buffer keeps these together as one browsable entry so the user can read
/// the card's identity in a single review step.
/// </summary>
public sealed class HeaderAnnouncement : Announcement
{
    private readonly Message _header;

    public HeaderAnnouncement(string header) : this(Message.Raw(header)) { }
    public HeaderAnnouncement(Message header) { _header = header; }

    public override string Key => "header";
    public override Message Render(AnnouncementContext ctx) => _header;
}
