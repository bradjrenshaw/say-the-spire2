using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Hotkey-context announcement listing each relic that currently shows a
/// counter, one line per relic ("Name, amount"). Construct with the already-
/// extracted (name, amount) pairs. Empty when no relic has an active counter,
/// letting the caller substitute the "no relic counters" message.
/// </summary>
public sealed class RelicCountersAnnouncement : Announcement
{
    private readonly IReadOnlyList<(string Name, int Amount)> _counters;

    public RelicCountersAnnouncement(IReadOnlyList<(string, int)> counters)
    {
        _counters = counters;
    }

    public override string Key => "relic_counters";

    public override Message Render(AnnouncementContext ctx) => Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        foreach (var (name, amount) in _counters)
            yield return Message.Localized("ui", "RELIC.COUNTER_NAMED", new { name, amount });
    }
}
