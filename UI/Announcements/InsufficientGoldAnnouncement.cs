using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>Announces that the player doesn't have enough gold for a shop item.</summary>
public sealed class InsufficientGoldAnnouncement : Announcement
{
    public override string Key => "insufficient_gold";
    public override string Suffix => ",";

    public override Message Render() =>
        Message.Localized("ui", "RESOURCE.NOT_ENOUGH_GOLD");
}
