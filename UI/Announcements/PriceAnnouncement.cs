using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>A shop item's gold price.</summary>
public sealed class PriceAnnouncement : Announcement
{
    private readonly int _cost;

    public PriceAnnouncement(int cost) { _cost = cost; }

    public override string Key => "price";
    public override string Suffix => ",";

    public override Message Render() =>
        Message.Localized("ui", "RESOURCE.PRICE", new { cost = _cost });
}
