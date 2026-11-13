using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Buffer-context announcement for the melodies the local player holds (the
/// Fool's Wah / Cho / Ki). Melodies aren't powers — the game works them out
/// from the cards played this turn and shows a symbol on the bow — but they
/// read like one here: a line per held melody with the game's hover tip,
/// "Wah Melody: Activates if you played a "Wah" card this turn."
///
/// The focus line stays empty; held melodies are already named in the
/// resources line (<see cref="ResourcesAnnouncement"/>).
/// </summary>
public sealed class MelodiesAnnouncement : Announcement
{
    private readonly MelodiesView _melodies;

    public MelodiesAnnouncement(MelodiesView melodies) { _melodies = melodies; }

    public override string Key => "melodies";

    public override Message Render(AnnouncementContext ctx) => Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        foreach (var (title, description) in _melodies.ActiveTips)
            yield return Message.Raw(string.IsNullOrEmpty(description) ? title : $"{title}: {description}");
    }
}
