using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// The local player's arrows (the Fool's resource): how many are in hand out
/// of how many they still have this combat, e.g. "1/2 arrows" when one is
/// stuck in an enemy.
/// </summary>
public sealed class ArrowsAnnouncement : Announcement
{
    private readonly int _available;
    private readonly int _total;

    public ArrowsAnnouncement(int available, int total)
    {
        _available = available;
        _total = total;
    }

    public override string Key => "arrows";
    public override string Suffix => ",";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "RESOURCE.ARROWS", new { current = _available, max = _total });
}
