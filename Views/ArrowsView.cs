using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;

namespace SayTheSpire2.Views;

/// <summary>
/// View over a player's arrows — the Fool's resource (November beta). Each
/// arrow slot is in hand, stuck in an enemy until dislodged, or destroyed for
/// the rest of the combat. The game draws one icon per slot beside the bow:
/// solid when in hand, faded when away, gone when destroyed.
///
/// Only the local player's arrows are viewable: the game sets that panel up
/// for your own character alone, so another Fool's arrow count is never
/// shown. <see cref="FromCombatState"/> returns null for other players, for
/// players with no arrow slots, and on stable (where arrows don't exist —
/// everything here is resolved by name).
/// </summary>
public sealed class ArrowsView
{
    // Beta-only; null on stable.
    private static readonly PropertyInfo? ArrowsProp =
        AccessTools.Property(typeof(PlayerCombatState), "Arrows");
    private static readonly Type? SlotType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.FoolArrows.ArrowSlot");

    // Resolved only where the type exists; a rename there should crash.
    private static readonly FieldInfo? OwnerField =
        SlotType == null ? null : AccessTools.Field(SlotType, "owner")!;
    private static readonly PropertyInfo? StatusProp =
        SlotType == null ? null : AccessTools.Property(SlotType, "Status")!;

    // ArrowStatus enum member names.
    private const string InHand = "Retrieved";
    private const string Destroyed = "Destroyed";

    private readonly PlayerCombatState _combatState;

    private ArrowsView(PlayerCombatState combatState) { _combatState = combatState; }

    public static ArrowsView? FromCombatState(PlayerCombatState? combatState)
    {
        if (combatState == null || Slots(combatState) is not { } slots)
            return null;

        foreach (var slot in slots)
        {
            // Any slot will do: they all belong to this combat state's player.
            return OwnerField?.GetValue(slot) is Player owner && LocalContext.IsMe(owner)
                ? new ArrowsView(combatState)
                : null;
        }
        return null;
    }

    /// <summary>Arrows in hand, ready to shoot.</summary>
    public int Available => Count(status => status == InHand);

    /// <summary>Arrows the player still has this combat: in hand or stuck in an enemy.</summary>
    public int Total => Count(status => status != Destroyed);

    private int Count(Func<string, bool> predicate)
    {
        var count = 0;
        if (Slots(_combatState) is { } slots)
        {
            foreach (var slot in slots)
            {
                if (StatusProp?.GetValue(slot)?.ToString() is { } status && predicate(status))
                    count++;
            }
        }
        return count;
    }

    private static IEnumerable? Slots(PlayerCombatState combatState) =>
        ArrowsProp?.GetValue(combatState) as IEnumerable;
}
