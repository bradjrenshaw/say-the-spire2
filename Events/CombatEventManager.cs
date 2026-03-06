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
    private static readonly HashSet<Creature> _subscribedCreatures = new();

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
            if (_subscribedCreatures.Contains(creature)) continue;
            SubscribeToCreature(creature);
        }
    }

    private static void SubscribeToCreature(Creature creature)
    {
        _subscribedCreatures.Add(creature);
        creature.BlockChanged += (oldBlock, newBlock) => Dispatch(new BlockEvent(creature, oldBlock, newBlock));
        creature.PowerIncreased += (power, change, silent) =>
        {
            if (!silent) Dispatch(new PowerEvent(creature, power, PowerEventType.Increased, change));
        };
        creature.PowerDecreased += (power, silent) =>
        {
            if (!silent) Dispatch(new PowerEvent(creature, power, PowerEventType.Decreased));
        };
        creature.PowerRemoved += power => Dispatch(new PowerEvent(creature, power, PowerEventType.Removed));
        creature.Died += c => Dispatch(new DeathEvent(c));
    }

    private static void UnsubscribeAll()
    {
        // Events are garbage collected with the creatures; just clear our tracking set
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
}
