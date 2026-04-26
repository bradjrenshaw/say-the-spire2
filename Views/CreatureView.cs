using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
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
    public Creature Entity { get; }

    private CreatureView(Creature entity) { Entity = entity; }

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

            var allies = Entity.CombatState?.Allies ?? Enumerable.Empty<Creature>();
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
