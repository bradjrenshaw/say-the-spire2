using System.Collections.Generic;
using System.Linq;
using SayTheSpire2.Localization;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Hotkey-context announcement for every living enemy's intents — one line
/// per enemy, "Name: intent1, intent2". Distinct from
/// <see cref="MonsterIntentsAnnouncement"/>, which renders a single focused
/// creature's intents. Construct with the already-extracted per-enemy intent
/// views so the announcement stays free of combat-state plumbing.
/// </summary>
public sealed class AllIntentsAnnouncement : Announcement
{
    private readonly IReadOnlyList<(string Name, IReadOnlyList<IntentView> Intents)> _enemies;

    public AllIntentsAnnouncement(IReadOnlyList<(string, IReadOnlyList<IntentView>)> enemies)
    {
        _enemies = enemies;
    }

    public override string Key => "all_intents";

    public override Message Render(AnnouncementContext ctx) => Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        foreach (var (name, intents) in _enemies)
        {
            var parts = intents
                .Select(i => !string.IsNullOrEmpty(i.Label) ? $"{i.Name} {i.Label}" : i.Name)
                .ToList();
            var body = parts.Count > 0
                ? string.Join(", ", parts)
                : LocalizationManager.GetOrDefault("ui", "LABELS.UNKNOWN", "Unknown");
            yield return Message.Raw($"{name}: {body}");
        }
    }
}
