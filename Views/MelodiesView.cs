using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Views;

/// <summary>
/// View over a player's melodies — the Fool's second mechanic (November
/// beta). There are three, Wah, Cho and Ki; you have one for the rest of the
/// turn once you've played a card of that kind this turn, and cards check for
/// them ("Costs 2 less for each Melody you have"). The game shows a symbol on
/// the bow for each one held. Those symbols are mouse-hover only, so there is
/// nothing to focus and the mod reads the state itself.
///
/// Only the local player's melodies are viewable: the game sets that panel up
/// for your own character alone. <see cref="FromCombatState"/> returns null
/// for other players and on stable, where melodies don't exist (everything
/// here is resolved by name).
/// </summary>
public sealed class MelodiesView
{
    // Beta-only; null on stable.
    private static readonly Type? MelodyCmdType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Commands.MelodyCmd");

    // Resolved only where the type exists; a rename there should crash.
    private static readonly MethodInfo? PlayedThisTurnMethod =
        MelodyCmdType == null ? null : AccessTools.Method(MelodyCmdType, "PlayedThisTurn")!;

    // Exists on both branches.
    private static readonly FieldInfo PlayerField =
        AccessTools.Field(typeof(PlayerCombatState), "_player")!;

    // CardTag members, in the order the game lays the symbols out.
    private static readonly string[] TagNames = { "Wah", "Cho", "Ki" };

    private readonly Player _player;

    private MelodiesView(Player player) { _player = player; }

    public static MelodiesView? FromCombatState(PlayerCombatState? combatState)
    {
        if (combatState == null || PlayedThisTurnMethod == null)
            return null;
        return PlayerField.GetValue(combatState) is Player player && LocalContext.IsMe(player)
            ? new MelodiesView(player)
            : null;
    }

    /// <summary>
    /// The game's own name for a melody ("Wah Melody"), from its card tag
    /// value or that value's name.
    /// </summary>
    public static string TitleOf(object tag) =>
        new LocString("static_hover_tips", tag.ToString()?.ToUpperInvariant() + "_MELODY.title").GetFormattedText();

    /// <summary>
    /// The game's hover tip text for a melody ("Activates if you played a
    /// "Wah" card this turn."), the same tip its symbol shows on mouse hover.
    /// </summary>
    public static string DescriptionOf(object tag) =>
        new LocString("static_hover_tips", tag.ToString()?.ToUpperInvariant() + "_MELODY.description").GetFormattedText();

    /// <summary>Titles of the melodies the player holds right now.</summary>
    public IReadOnlyList<string> ActiveTitles
    {
        get
        {
            var titles = new List<string>();
            foreach (var tag in ActiveTags())
                titles.Add(TitleOf(tag));
            return titles;
        }
    }

    /// <summary>
    /// Title and hover tip description of each melody the player holds right now.
    /// </summary>
    public IReadOnlyList<(string Title, string Description)> ActiveTips
    {
        get
        {
            var tips = new List<(string, string)>();
            foreach (var tag in ActiveTags())
                tips.Add((TitleOf(tag), DescriptionOf(tag)));
            return tips;
        }
    }

    // The CardTag values of the melodies held right now, boxed.
    private List<object> ActiveTags()
    {
        var tags = new List<object>();
        if (PlayedThisTurnMethod == null)
            return tags;

        var tagType = PlayedThisTurnMethod.GetParameters()[1].ParameterType;
        foreach (var name in TagNames)
        {
            try
            {
                var tag = Enum.Parse(tagType, name);
                if (PlayedThisTurnMethod.Invoke(null, new[] { _player, tag }) is true)
                    tags.Add(tag);
            }
            catch (Exception e)
            {
                // The game reads this from the current combat's history;
                // outside a combat there is none to ask.
                Log.Info($"[AccessibilityMod] Melody lookup failed for {name}: {e.Message}");
            }
        }
        return tags;
    }
}
