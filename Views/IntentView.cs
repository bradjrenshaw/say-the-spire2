using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Views;

/// <summary>
/// A single intent belonging to a monster's next move. Structured data only —
/// callers do their own formatting.
/// </summary>
public record IntentView(string Name, string? Label, string? Description = null)
{
    private static readonly PropertyInfo? IntentTitleProp =
        AccessTools.Property(typeof(AbstractIntent), "IntentTitle");

    public static IntentView FromIntent(AbstractIntent intent, Creature owner, IEnumerable<Creature>? allies = null)
    {
        var name = GetIntentName(intent);
        var label = intent.GetIntentLabel(allies ?? Enumerable.Empty<Creature>(), owner);
        var text = label.GetFormattedText();

        // Fetch the description via the same hover-tip path the game uses in
        // the UI. Used by buffer-mode rendering to give a per-intent line
        // that's richer than the focus summary.
        string? description = null;
        try
        {
            var tip = intent.GetHoverTip(allies ?? Enumerable.Empty<Creature>(), owner);
            if (!string.IsNullOrEmpty(tip.Description))
                description = Message.StripBbcode(tip.Description);
        }
        catch (Exception e) { Log.Info($"[AccessibilityMod] Intent hover-tip description fetch failed: {e.Message}"); }

        return new IntentView(name, string.IsNullOrEmpty(text) ? null : Message.StripBbcode(text), description);
    }

    /// <summary>
    /// Gets the game's localized intent title. Falls back to the IntentType enum name
    /// if reflection fails — the fallback is still meaningful, so this intentionally
    /// degrades gracefully rather than crashing.
    /// </summary>
    public static string GetIntentName(AbstractIntent intent)
    {
        try
        {
            var locString = IntentTitleProp?.GetValue(intent) as MegaCrit.Sts2.Core.Localization.LocString;
            var title = locString?.GetFormattedText();
            if (!string.IsNullOrEmpty(title))
                return title;
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Intent title lookup failed: {e.Message}"); }
        return intent.IntentType.ToString();
    }
}
