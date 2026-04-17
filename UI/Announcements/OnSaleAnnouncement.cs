using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>Announces that a shop item is on sale.</summary>
public sealed class OnSaleAnnouncement : Announcement
{
    public override string Key => "on_sale";
    public override string Suffix => ",";

    public override Message Render() =>
        Message.Localized("ui", "RESOURCE.ON_SALE");
}
