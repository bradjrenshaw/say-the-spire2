using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using SayTheSpire2.Localization;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI;

/// <summary>
/// Builds resource strings from combat state. Centralizes formatting so
/// energy, stars, and any future resources are consistent everywhere.
/// </summary>
public static class ResourceHelper
{
    public static Message GetResourceMessage(PlayerCombatState pcs)
    {
        var parts = new List<Message>
        {
            Message.Localized("ui", "RESOURCE.ENERGY", new { current = pcs.Energy, max = pcs.MaxEnergy }),
        };
        if (pcs.Stars > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.STARS", new { amount = pcs.Stars }));
        if (ArrowsView.FromCombatState(pcs) is { } arrows)
            parts.Add(Message.Localized("ui", "RESOURCE.ARROWS", new { current = arrows.Available, max = arrows.Total }));
        if (MelodiesView.FromCombatState(pcs) is { } melodies)
        {
            foreach (var title in melodies.ActiveTitles)
                parts.Add(Message.Raw(title));
        }
        return Message.Join(", ", parts.ToArray());
    }
}
