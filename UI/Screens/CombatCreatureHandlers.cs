using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.Events;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Handles creature events (HP, block, power, death) for combat.
/// Extracted from CombatScreen for clarity.
/// </summary>
internal class CombatCreatureHandlers
{
    private readonly Creature _creature;

    public CombatCreatureHandlers(Creature creature)
    {
        _creature = creature;
    }

    public void OnBlockChanged(int oldBlock, int newBlock)
    {
        Log.Info($"[EventDebug] CreatureHandler.BlockChanged: {_creature.Name} {oldBlock}->{newBlock} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new BlockEvent(_creature, oldBlock, newBlock));
    }

    public void OnCurrentHpChanged(int oldHp, int newHp)
    {
        Log.Info($"[EventDebug] CreatureHandler.HpChanged: {_creature.Name} {oldHp}->{newHp} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new HpEvent(_creature, oldHp, newHp));
    }

    public void OnPowerIncreased(PowerModel power, int change, bool silent)
    {
        Log.Info($"[EventDebug] CreatureHandler.PowerIncreased: {_creature.Name} {power.Title.GetFormattedText()} +{change} silent={silent} handler={GetHashCode()}");
        if (!silent) EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Increased, change));
    }

    public void OnPowerDecreased(PowerModel power, bool silent)
    {
        Log.Info($"[EventDebug] CreatureHandler.PowerDecreased: {_creature.Name} {power.Title.GetFormattedText()} amount={power.Amount} silent={silent} handler={GetHashCode()}");
        if (silent || power.Amount == 0) return;
        // Single-stack powers (Shrink, etc.) fire PowerDecreased when they're
        // first applied with a negative sentinel amount because the game's
        // InvokePowerModified treats the negative delta as a decrease. There
        // is no meaningful "decreased" state for these — they're either
        // applied or removed. PowerApplied (subscribed separately) handles
        // the initial announcement.
        if (power.StackType != MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter) return;
        EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Decreased));
    }

    public void OnPowerApplied(PowerModel power)
    {
        Log.Info($"[EventDebug] CreatureHandler.PowerApplied: {_creature.Name} {power.Title.GetFormattedText()} handler={GetHashCode()}");
        // Counter-stack powers already get a usable PowerIncreased event (with
        // the +N stack count), so we'd double-announce if we also fired here.
        // Only Single-stack powers need the Applied path — for those the
        // matching PowerDecreased was skipped above.
        if (power.StackType == MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter) return;
        EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Applied));
    }

    public void OnPowerRemoved(PowerModel power)
    {
        Log.Info($"[EventDebug] CreatureHandler.PowerRemoved: {_creature.Name} {power.Title.GetFormattedText()} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Removed));
    }

    public void OnDied(Creature c)
    {
        Log.Info($"[EventDebug] CreatureHandler.Died: {c.Name} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new DeathEvent(c));
    }
}
