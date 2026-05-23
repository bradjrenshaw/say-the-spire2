using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Hotkey-context summarized-intents announcement: the total incoming damage
/// across all enemies this turn, or a "no incoming damage" message when zero.
/// Unlike the list announcements this always says something, so it renders a
/// real message rather than going empty.
/// </summary>
public sealed class IncomingDamageAnnouncement : Announcement
{
    private readonly int _totalDamage;

    public IncomingDamageAnnouncement(int totalDamage) { _totalDamage = totalDamage; }

    public override string Key => "incoming_damage";

    public override Message Render(AnnouncementContext ctx) =>
        _totalDamage > 0
            ? Message.Localized("ui", "SPEECH.INCOMING_DAMAGE", new { amount = _totalDamage })
            : Message.Localized("ui", "SPEECH.NO_INCOMING_DAMAGE");
}
