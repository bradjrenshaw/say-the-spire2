using System.Collections.Generic;
using System.Linq;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context tail: caller-supplied extra lines (e.g. library stats on
/// card grid views). Each non-empty line becomes its own browsable entry.
/// Always appears last in the buffer order by convention so it doesn't
/// interleave with the model's own data.
/// </summary>
public sealed class ExtrasAnnouncement : Announcement
{
    private readonly IReadOnlyList<string> _lines;

    public ExtrasAnnouncement(IEnumerable<string>? lines)
    {
        _lines = lines?
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList()
            ?? new List<string>();
    }

    public override string Key => "extras";

    public override Message Render(AnnouncementContext ctx) =>
        _lines.Count > 0 ? Message.Raw(_lines[0]) : Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        foreach (var line in _lines)
            yield return Message.Raw(line);
    }
}
