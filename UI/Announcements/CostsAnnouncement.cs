using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context costs line: a card's combined energy + star cost joined
/// with commas (e.g. "1 energy, 2 stars"). The focus path uses
/// EnergyCostAnnouncement, which has its own verbose/non-verbose modes;
/// the buffer always renders verbose since each entry is a standalone
/// browsable line.
/// </summary>
public sealed class CostsAnnouncement : Announcement
{
    private readonly Message? _line;

    /// <summary>Pass the already-joined costs string (empty/null skips the line).</summary>
    public CostsAnnouncement(string? line)
    {
        _line = string.IsNullOrEmpty(line) ? null : Message.Raw(line);
    }

    public override string Key => "costs";
    public override Message Render(AnnouncementContext ctx) => _line ?? Message.Empty;
}
