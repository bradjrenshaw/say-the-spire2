using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A player's current gold total. Used in buffer contexts where gold sits
/// among the resource summary lines.
/// </summary>
public sealed class GoldAnnouncement : Announcement
{
    private readonly int _amount;

    public GoldAnnouncement(int amount) { _amount = amount; }

    public override string Key => "gold";
    public override Message Render(AnnouncementContext ctx) =>
        Message.Localized("ui", "RESOURCE.GOLD", new { amount = _amount });
}
