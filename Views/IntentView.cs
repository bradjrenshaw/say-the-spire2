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
public record IntentView(string Name, string? Label)
{
    private static readonly PropertyInfo? IntentTitleProp =
        AccessTools.Property(typeof(AbstractIntent), "IntentTitle");

    public static IntentView FromIntent(AbstractIntent intent, Creature owner, IEnumerable<Creature>? allies = null)
    {
        var name = GetIntentName(intent);
        var label = intent.GetIntentLabel(allies ?? Enumerable.Empty<Creature>(), owner);
        var text = label.GetFormattedText();
        return new IntentView(name, string.IsNullOrEmpty(text) ? null : Message.StripBbcode(text));
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
