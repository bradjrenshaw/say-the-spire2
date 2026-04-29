using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.Views;

/// <summary>
/// Data wrapper over a game Creature. Owns the reflection and parent-walking
/// logic for creature lookups. Exposes typed properties — no speech, no
/// localization, no settings awareness.
/// </summary>
public class CreatureView
{
    // Resolve Creature.CombatState reflectively. The game shifted from
    // returning a concrete CombatState to an ICombatState interface across
    // a beta update; embedding either return type in the mod's IL would
    // throw MissingMethodException on the other version at JIT time. Name-
    // based reflection sidesteps the signature check entirely.
    private static readonly PropertyInfo? CombatStateProperty =
        AccessTools.Property(typeof(Creature), "CombatState");

    public Creature Entity { get; }

    private CreatureView(Creature entity) { Entity = entity; }

    /// <summary>
    /// The allies of this creature's combat state, or an empty sequence
    /// when not in combat. Reflection-based to avoid baking the
    /// CombatState property's return type into the shipped DLL.
    /// </summary>
    public static IEnumerable<Creature> GetCombatStateAllies(Creature entity)
    {
        var combatState = CombatStateProperty?.GetValue(entity);
        if (combatState == null) return Array.Empty<Creature>();
        var alliesProp = AccessTools.Property(combatState.GetType(), "Allies");
        return alliesProp?.GetValue(combatState) as IReadOnlyList<Creature>
            ?? Array.Empty<Creature>();
    }

    private static IReadOnlyList<Creature> GetCombatStateOpponents(Creature entity)
    {
        var combatState = CombatStateProperty?.GetValue(entity);
        if (combatState == null) return Array.Empty<Creature>();

        var getOpponents = AccessTools.Method(combatState.GetType(), "GetOpponentsOf", new[] { typeof(Creature) });
        return getOpponents?.Invoke(combatState, new object[] { entity }) as IReadOnlyList<Creature>
            ?? Array.Empty<Creature>();
    }

    /// <summary>
    /// Walks up from a Godot control to find the enclosing NCreature and returns
    /// a view of its entity. Null if no NCreature ancestor or the entity is unset.
    /// </summary>
    public static CreatureView? FromControl(Control? control)
    {
        if (control == null) return null;
        var node = FindAncestor<NCreature>(control);
        var entity = node?.Entity;
        return entity == null ? null : new CreatureView(entity);
    }

    public static CreatureView FromEntity(Creature entity) => new(entity);

    public string Name => MultiplayerHelper.GetCreatureName(Entity);
    public int CurrentHp => Entity.CurrentHp;
    public int MaxHp => Entity.MaxHp;
    public int Block => Entity.Block;
    public bool IsPlayer => Entity.IsPlayer;
    public bool IsMonster => Entity.IsMonster;
    public bool IsLocalPlayer => LocalContext.IsMe(Entity);
    public Player? Player => Entity.Player;
    public MonsterModel? Monster => Entity.Monster;

    public IReadOnlyList<Creature> SurroundedFacingTargets
    {
        get
        {
            var surrounded = Entity.Powers.OfType<SurroundedPower>().FirstOrDefault();
            if (surrounded == null) return Array.Empty<Creature>();

            var opponents = GetCombatStateOpponents(Entity);
            return surrounded.Facing switch
            {
                SurroundedPower.Direction.Right => opponents
                    .Where(c => c.IsAlive && c.HasPower<BackAttackRightPower>())
                    .ToList(),
                SurroundedPower.Direction.Left => opponents
                    .Where(c => c.IsAlive && c.HasPower<BackAttackLeftPower>())
                    .ToList(),
                _ => Array.Empty<Creature>(),
            };
        }
    }

    /// <summary>
    /// The non-local player who owns this creature, when it's a pet in a real
    /// multiplayer session. Null in singleplayer, for non-pets, or when the
    /// pet is owned by the local player (where labelling the owner is noise).
    /// </summary>
    public Player? OtherPlayerPetOwner
    {
        get
        {
            var owner = Entity.PetOwner;
            if (owner == null) return null;
            try
            {
                if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer) return null;
            }
            catch (System.Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] OtherPlayerPetOwner singleplayer check failed: {e.Message}");
                return null;
            }
            return LocalContext.IsMe(owner) ? null : owner;
        }
    }

    /// <summary>
    /// The player's current combat state (energy, stars, hand). Null for
    /// non-players, or when the player isn't in combat.
    /// </summary>
    public PlayerCombatState? PlayerCombatState => IsPlayer ? Player?.PlayerCombatState : null;

    /// <summary>
    /// The monster's current intents (what it will do next turn). Empty for non-monsters
    /// or monsters without a queued move.
    /// </summary>
    public IReadOnlyList<IntentView> MonsterIntents
    {
        get
        {
            if (!IsMonster || Monster == null) return Array.Empty<IntentView>();
            var intents = Monster.NextMove?.Intents;
            if (intents == null || intents.Count == 0) return Array.Empty<IntentView>();

            var allies = GetCombatStateAllies(Entity);
            var result = new List<IntentView>(intents.Count);
            foreach (var intent in intents)
                result.Add(IntentView.FromIntent(intent, Entity, allies));
            return result;
        }
    }

    /// <summary>
    /// The model the player is currently hovering over (card/relic/potion/power).
    /// Null for non-players or when nothing is hovered.
    /// </summary>
    public AbstractModel? PlayerHoveredModel
    {
        get
        {
            if (!IsPlayer || Player == null) return null;
            return RunManager.Instance?.HoveredModelTracker?.GetHoveredModel(Player.NetId);
        }
    }

    private static T? FindAncestor<T>(Node? node) where T : class
    {
        var current = node;
        while (current != null)
        {
            if (current is T match) return match;
            current = current.GetParent();
        }
        return null;
    }
}
