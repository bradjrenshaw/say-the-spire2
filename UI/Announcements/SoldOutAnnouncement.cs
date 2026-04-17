using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>Announces that a shop item has already been purchased / used.</summary>
public sealed class SoldOutAnnouncement : Announcement
{
    public override string Key => "sold_out";
    public override string Suffix => ",";

    public override Message Render() =>
        Message.Localized("ui", "LABELS.SOLD_OUT");
}
