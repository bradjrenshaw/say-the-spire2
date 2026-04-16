using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI;

/// <summary>
/// Formats a creature's next-action summary ("what is this creature about to do")
/// into a speech-ready string. For monsters, this is the queued intents; for
/// players, it's the model they're currently hovering. Lives in UI/ because
/// player-intent formatting dispatches into proxies. Expected to be absorbed by
/// IntentsAnnouncement during Phase 2 of the focus announcements refactor.
/// </summary>
public static class CreatureIntentFormatter
{
    public static string? Summary(CreatureView view, bool includePrefix = true)
    {
        try
        {
            if (view.IsMonster)
                return MonsterSummary(view, includePrefix);
            if (view.IsPlayer && view.Player != null)
                return PlayerSummary(view, includePrefix);
        }
        catch (Exception e)
        {
            Log.Info($"[AccessibilityMod] Intent summary build failed: {e.Message}");
        }
        return null;
    }

    private static string? MonsterSummary(CreatureView view, bool includePrefix)
    {
        var intents = view.MonsterIntents;
        if (intents.Count == 0) return null;

        var summaries = intents.Select(intent =>
            !string.IsNullOrEmpty(intent.Label)
                ? $"{intent.Name} {intent.Label}"
                : intent.Name);

        var joined = string.Join(", ", summaries);
        return includePrefix
            ? LocalizationManager.GetOrDefault("ui", "CREATURE.INTENT_PREFIX", "Intent") + " " + joined
            : joined;
    }

    private static string? PlayerSummary(CreatureView view, bool includePrefix)
    {
        var model = view.PlayerHoveredModel;
        if (model == null) return null;

        var summary = HoveredModelSummary(model);
        if (string.IsNullOrWhiteSpace(summary)) return null;

        return includePrefix
            ? LocalizationManager.GetOrDefault("ui", "CREATURE.INTENT_PREFIX", "Intent") + " " + summary
            : summary;
    }

    public static string? HoveredModelSummary(AbstractModel model)
    {
        return model switch
        {
            CardModel card => CardSummary(card),
            RelicModel relic => RelicSummary(relic),
            PotionModel potion => PotionSummary(potion),
            PowerModel power => PowerSummary(power),
            _ => null,
        };
    }

    private static string CardSummary(CardModel card)
    {
        var proxy = ProxyCard.FromModel(card);
        var parts = new List<string>();
        var label = proxy.GetLabel()?.Resolve();
        var extras = proxy.GetExtrasString()?.Resolve();
        var subtype = proxy.GetSubtypeKey();

        if (!string.IsNullOrWhiteSpace(label))
            parts.Add(label);
        if (!string.IsNullOrWhiteSpace(extras))
            parts.Add(extras);
        if (!string.IsNullOrWhiteSpace(subtype))
            parts.Add(Message.Localized("ui", "CREATURE.SUBTYPE_CARD", new { subtype }).Resolve());

        return string.Join(", ", parts);
    }

    private static string RelicSummary(RelicModel relic)
    {
        var proxy = ProxyRelicHolder.FromModel(relic);
        var parts = new List<string>();
        var label = proxy.GetLabel()?.Resolve();
        var status = proxy.GetStatusString()?.Resolve();

        if (!string.IsNullOrWhiteSpace(label))
            parts.Add(label);
        if (!string.IsNullOrWhiteSpace(status))
            parts.Add(status);

        return string.Join(", ", parts);
    }

    private static string PotionSummary(PotionModel potion)
    {
        return ProxyPotionHolder.FromModel(potion).GetLabel()?.Resolve() ?? potion.Title.GetFormattedText();
    }

    private static string PowerSummary(PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        if (power.StackType == PowerStackType.Counter && power.DisplayAmount != 0)
            return $"{title} {power.DisplayAmount}";
        return title;
    }
}
