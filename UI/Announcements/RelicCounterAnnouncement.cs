using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A relic's current counter value (e.g., "counter 3"). Rendered from the
/// RELIC.COUNTER localization template.
/// </summary>
public sealed class RelicCounterAnnouncement : Announcement
{
    private readonly int _amount;

    public RelicCounterAnnouncement(int amount) { _amount = amount; }

    public override string Key => "relic_counter";
    public override string Suffix => ",";

    public override Message Render() =>
        Message.Localized("ui", "RELIC.COUNTER", new { amount = _amount });
}
