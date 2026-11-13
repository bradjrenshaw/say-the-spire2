using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.Views;

/// <summary>
/// View over a game PowerModel, covering whether the mod should read it.
///
/// The game hides some powers from the local player (<c>PowerModel.IsVisible</c>):
/// in multiplayer a creature can carry one copy of a power per player, and each
/// player is shown only their own — a power aimed at another player, or one
/// only its applier is meant to see (the Fool's Impaled). The mod follows that
/// rule, so a creature no longer reads as "Sandpit 3, Sandpit 4, Sandpit 2".
///
/// <see cref="ShownWhenHidden"/> lists the exceptions: powers the game hides
/// but conveys some other way, which are read anyway with the player they
/// concern named.
/// </summary>
public sealed class PowerView
{
    /// <summary>
    /// Powers read even when the game hides them from the local player.
    /// Add a type here only when sighted players can still see the same
    /// information without the icon.
    ///
    /// Sandpit (The Insatiable): one copy per player, counting the turns until
    /// that player is eaten. Everyone can see everyone's countdown, because
    /// each player's character is dragged toward the boss as theirs drops.
    /// </summary>
    private static readonly HashSet<Type> ShownWhenHidden = new()
    {
        typeof(SandpitPower),
    };

    public PowerModel Entity { get; }

    private PowerView(PowerModel power) { Entity = power; }

    public static PowerView FromModel(PowerModel power) => new(power);

    /// <summary>The powers on a creature that the mod reads, in the game's order.</summary>
    public static IEnumerable<PowerView> ShownOn(Creature creature) =>
        Shown(creature.Powers);

    public static IEnumerable<PowerView> Shown(IEnumerable<PowerModel> powers) =>
        powers.Select(FromModel).Where(p => p.IsShown);

    /// <summary>Whether the game shows this power to the local player.</summary>
    public bool IsVisibleInGame => Entity.IsVisible;

    /// <summary>Hidden by the game but on the exceptions list.</summary>
    public bool IsHiddenException => !IsVisibleInGame && ShownWhenHidden.Contains(Entity.GetType());

    /// <summary>Whether the mod reads this power at all.</summary>
    public bool IsShown => IsVisibleInGame || IsHiddenException;

    /// <summary>
    /// For a hidden-exception power, the name of the player it concerns: the
    /// one it is aimed at, else the one who applied it. Null for powers the
    /// game shows normally (those are the local player's own, or not
    /// player-specific).
    /// </summary>
    public string? OtherPlayerName
    {
        get
        {
            if (!IsHiddenException)
                return null;
            var concerned = Entity.Target ?? Entity.Applier;
            return concerned == null ? null : MultiplayerHelper.GetCreatureName(concerned);
        }
    }
}
