using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Sts2AccessibilityMod.Buffers;
using Sts2AccessibilityMod.Speech;

namespace Sts2AccessibilityMod.Events;

public static class CombatEventManager
{
    private static readonly Dictionary<Creature, CreatureHandlers> _subscribedCreatures = new();

    public static void Initialize()
    {
        var cm = CombatManager.Instance;
        cm.CombatSetUp += OnCombatSetUp;
        cm.CombatEnded += OnCombatEnded;
        cm.CreaturesChanged += OnCreaturesChanged;
        cm.TurnStarted += OnTurnStarted;
        Log.Info("[AccessibilityMod] CombatEventManager initialized.");
    }

    private static void OnCombatSetUp(CombatState state)
    {
        SubscribeToAllCreatures(state);
    }

    private static void OnCombatEnded(CombatRoom _)
    {
        UnsubscribeAll();
    }

    private static void OnCreaturesChanged(CombatState state)
    {
        SubscribeToAllCreatures(state);
    }

    private static void OnTurnStarted(CombatState state)
    {
        Dispatch(new TurnEvent(state.CurrentSide, state.RoundNumber, isStart: true));
    }

    private static void SubscribeToAllCreatures(CombatState state)
    {
        foreach (var creature in state.Creatures)
        {
            if (_subscribedCreatures.ContainsKey(creature)) continue;
            SubscribeToCreature(creature);
        }
    }

    private static void SubscribeToCreature(Creature creature)
    {
        var handlers = new CreatureHandlers(creature);
        _subscribedCreatures[creature] = handlers;

        creature.BlockChanged += handlers.OnBlockChanged;
        creature.PowerIncreased += handlers.OnPowerIncreased;
        creature.PowerDecreased += handlers.OnPowerDecreased;
        creature.PowerRemoved += handlers.OnPowerRemoved;
        creature.Died += handlers.OnDied;
    }

    private static void UnsubscribeAll()
    {
        foreach (var (creature, handlers) in _subscribedCreatures)
        {
            creature.BlockChanged -= handlers.OnBlockChanged;
            creature.PowerIncreased -= handlers.OnPowerIncreased;
            creature.PowerDecreased -= handlers.OnPowerDecreased;
            creature.PowerRemoved -= handlers.OnPowerRemoved;
            creature.Died -= handlers.OnDied;
        }
        _subscribedCreatures.Clear();
    }

    private static void Dispatch(GameEvent evt)
    {
        var message = evt.GetMessage();
        if (string.IsNullOrEmpty(message)) return;

        if (evt.ShouldAnnounce())
        {
            SpeechManager.Output(message, interrupt: false);
        }

        if (evt.ShouldAddToBuffer())
        {
            var buffer = BufferManager.Instance.GetBuffer("events");
            buffer?.Add(message);
            BufferManager.Instance.EnableBuffer("events", true);
        }
    }

    private class CreatureHandlers
    {
        private readonly Creature _creature;

        public CreatureHandlers(Creature creature)
        {
            _creature = creature;
        }

        public void OnBlockChanged(int oldBlock, int newBlock)
        {
            Dispatch(new BlockEvent(_creature, oldBlock, newBlock));
        }

        public void OnPowerIncreased(PowerModel power, int change, bool silent)
        {
            if (!silent) Dispatch(new PowerEvent(_creature, power, PowerEventType.Increased, change));
        }

        public void OnPowerDecreased(PowerModel power, bool silent)
        {
            if (!silent) Dispatch(new PowerEvent(_creature, power, PowerEventType.Decreased));
        }

        public void OnPowerRemoved(PowerModel power)
        {
            Dispatch(new PowerEvent(_creature, power, PowerEventType.Removed));
        }

        public void OnDied(Creature c)
        {
            Dispatch(new DeathEvent(c));
        }
    }
}
