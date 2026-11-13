using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// A character's Dread win streak in the Dread lobby: the current streak (the
/// game draws it as a chain, one link per win) and the best-streak records,
/// e.g. "Streak 2, Best Streak 5". All game wording; see
/// <see cref="Views.DreadLobbyView.StreakLinesFor"/>.
/// </summary>
public sealed class DreadStreakAnnouncement : Announcement
{
    private readonly IReadOnlyList<string> _lines;

    public DreadStreakAnnouncement(IReadOnlyList<string> lines) { _lines = lines; }

    public override string Key => "dread_streak";
    public override string Suffix => ",";

    public override Message Render(AnnouncementContext ctx) =>
        Message.Raw(string.Join(", ", _lines));

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        foreach (var line in _lines)
            yield return Message.Raw(line);
    }
}
