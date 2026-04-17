using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Announces that a relic is currently in the disabled state. Stateless — the
/// caller only yields this when the relic is actually disabled.
/// </summary>
public sealed class RelicDisabledAnnouncement : Announcement
{
    public override string Key => "relic_disabled";
    public override string Suffix => ",";

    public override Message Render() =>
        Message.Localized("ui", "RELIC.DISABLED");
}
