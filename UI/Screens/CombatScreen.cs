using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Input;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Screens;

public class CombatScreen : Screen
{
    public static CombatScreen? Current { get; private set; }

    private readonly CombatState _initialState;
    private readonly Dictionary<Creature, CreatureHandlers> _subscribedCreatures = new();
    private CardPileHandlers? _cardPileHandlers;

    public CombatScreen(CombatState state)
    {
        _initialState = state;
        ClaimAction("announce_block");
        ClaimAction("announce_energy");
        ClaimAction("announce_powers");
        ClaimAction("announce_intents");
    }

    public override void OnPush()
    {
        Current = this;
        SubscribeToAllCreatures(_initialState);
        CombatManager.Instance.CreaturesChanged += OnCreaturesChanged;
        CombatManager.Instance.TurnStarted += OnTurnStarted;

        var player = GetLocalPlayer();
        if (player?.PlayerCombatState != null)
        {
            _cardPileHandlers = new CardPileHandlers(player.PlayerCombatState);
            _cardPileHandlers.Subscribe();
        }

        Log.Info("[AccessibilityMod] CombatScreen pushed.");
    }

    public override void OnPop()
    {
        CombatManager.Instance.CreaturesChanged -= OnCreaturesChanged;
        CombatManager.Instance.TurnStarted -= OnTurnStarted;
        _cardPileHandlers?.Unsubscribe();
        _cardPileHandlers = null;
        UnsubscribeAll();
        if (Current == this) Current = null;
        Log.Info("[AccessibilityMod] CombatScreen popped.");
    }

    // -- Shortcut announcements --

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "announce_block":
                AnnounceBlock();
                return true;
            case "announce_energy":
                AnnounceEnergy();
                return true;
            case "announce_powers":
                AnnouncePowers();
                return true;
            case "announce_intents":
                AnnounceIntents();
                return true;
        }

        return false;
    }

    private void AnnounceBlock()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        SpeechManager.Output($"{player.Creature.Block} block");
    }

    private void AnnounceEnergy()
    {
        var player = GetLocalPlayer();
        var combatState = player?.PlayerCombatState;
        if (combatState == null) return;
        SpeechManager.Output($"{combatState.Energy} of {combatState.MaxEnergy} energy");
    }

    private void AnnouncePowers()
    {
        var player = GetLocalPlayer();
        if (player == null) return;

        var powers = player.Creature.Powers;
        if (powers.Count == 0)
        {
            SpeechManager.Output("No powers");
            return;
        }

        var sb = new StringBuilder();
        foreach (var power in powers)
        {
            if (sb.Length > 0) sb.Append(", ");
            var title = power.Title.GetFormattedText();
            if (power.DisplayAmount != 0)
                sb.Append($"{title} {power.DisplayAmount}");
            else
                sb.Append(title);
        }
        SpeechManager.Output(sb.ToString());
    }

    private void AnnounceIntents()
    {
        var playerCreatures = new List<Creature>();
        var player = GetLocalPlayer();
        if (player != null)
            playerCreatures.Add(player.Creature);

        var sb = new StringBuilder();
        foreach (var enemy in _initialState.Enemies)
        {
            if (!enemy.IsAlive || !enemy.IsMonster) continue;

            if (sb.Length > 0) sb.Append(". ");
            sb.Append(enemy.Name);
            sb.Append(": ");

            var move = enemy.Monster!.NextMove;
            var intentParts = new List<string>();
            foreach (var intent in move.Intents)
            {
                var label = intent.GetIntentLabel(playerCreatures, enemy).GetFormattedText();
                if (!string.IsNullOrEmpty(label))
                    intentParts.Add(label);
                else
                    intentParts.Add(intent.IntentType.ToString());
            }
            sb.Append(intentParts.Count > 0 ? string.Join(", ", intentParts) : "Unknown");
        }

        if (sb.Length == 0)
        {
            SpeechManager.Output("No enemies");
            return;
        }

        SpeechManager.Output(sb.ToString());
    }

    private Player? GetLocalPlayer()
    {
        return LocalContext.GetMe(_initialState);
    }

    // -- Navigation fixes --

    public void OnCreatureNavigationUpdated(NCombatRoom combatRoom)
    {
        try
        {
            SetCreatureFocusToRelics(combatRoom);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Creature navigation postfix failed: {e.Message}");
        }
    }

    public void OnTargetingStarted()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            foreach (var creature in combatRoom.CreatureNodes)
            {
                if (creature == null) continue;
                var hitbox = creature.Hitbox;
                if (hitbox == null) continue;
                hitbox.FocusNeighborTop = hitbox.GetPath();
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] StartTargeting failed: {e.Message}");
        }
    }

    public void OnTargetingFinished()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;
            SetCreatureFocusToRelics(combatRoom);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] FinishTargeting failed: {e.Message}");
        }
    }

    public void OnHandLayoutRefreshed(NPlayerHand hand)
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            var firstCreature = combatRoom.CreatureNodes
                .FirstOrDefault(c => c != null && c.IsInteractable && c.Hitbox != null);
            if (firstCreature == null) return;

            var creaturePath = firstCreature.Hitbox.GetPath();

            foreach (var holder in hand.ActiveHolders)
            {
                if (holder == null) continue;
                holder.FocusNeighborTop = creaturePath;
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Hand navigation failed: {e.Message}");
        }
    }

    public void OnCardStolen(string cardName)
    {
        EventDispatcher.Enqueue(new CardStolenEvent(cardName));
    }

    // -- Navigation helpers --

    private static void SetCreatureFocusToRelics(NCombatRoom combatRoom)
    {
        var firstRelic = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes?.FirstOrDefault();
        if (firstRelic == null || !GodotObject.IsInstanceValid(firstRelic)) return;

        var relicPath = firstRelic.GetPath();

        foreach (var creature in combatRoom.CreatureNodes)
        {
            if (creature == null) continue;
            var hitbox = creature.Hitbox;
            if (hitbox == null) continue;
            hitbox.FocusNeighborTop = relicPath;
        }
    }

    // -- Combat event subscriptions --

    private void OnCreaturesChanged(CombatState state)
    {
        SubscribeToAllCreatures(state);
    }

    private void OnTurnStarted(CombatState state)
    {
        _cardPileHandlers?.OnTurnStarted();
        EventDispatcher.Enqueue(new TurnEvent(state.CurrentSide, state.RoundNumber, isStart: true));
    }

    public void OnShuffleStarted() => _cardPileHandlers?.OnShuffleStarted();
    public void OnShuffleFinished() => _cardPileHandlers?.OnShuffleFinished();

    private void SubscribeToAllCreatures(CombatState state)
    {
        foreach (var creature in state.Creatures)
        {
            if (_subscribedCreatures.ContainsKey(creature)) continue;
            SubscribeToCreature(creature);
        }
    }

    private void SubscribeToCreature(Creature creature)
    {
        var handlers = new CreatureHandlers(creature);
        _subscribedCreatures[creature] = handlers;

        creature.BlockChanged += handlers.OnBlockChanged;
        creature.CurrentHpChanged += handlers.OnCurrentHpChanged;
        creature.PowerIncreased += handlers.OnPowerIncreased;
        creature.PowerDecreased += handlers.OnPowerDecreased;
        creature.PowerRemoved += handlers.OnPowerRemoved;
        creature.Died += handlers.OnDied;
    }

    private void UnsubscribeAll()
    {
        foreach (var (creature, handlers) in _subscribedCreatures)
        {
            creature.BlockChanged -= handlers.OnBlockChanged;
            creature.CurrentHpChanged -= handlers.OnCurrentHpChanged;
            creature.PowerIncreased -= handlers.OnPowerIncreased;
            creature.PowerDecreased -= handlers.OnPowerDecreased;
            creature.PowerRemoved -= handlers.OnPowerRemoved;
            creature.Died -= handlers.OnDied;
        }
        _subscribedCreatures.Clear();
    }

    private class CardPileHandlers
    {
        private readonly PlayerCombatState _combatState;
        private bool _isShuffling;
        private bool _endOfTurnDiscardAnnounced;

        public CardPileHandlers(PlayerCombatState combatState)
        {
            _combatState = combatState;
        }

        public void Subscribe()
        {
            _combatState.Hand.CardAdded += OnHandCardAdded;
            _combatState.DiscardPile.CardAdded += OnDiscardCardAdded;
            _combatState.ExhaustPile.CardAdded += OnExhaustCardAdded;
            _combatState.DrawPile.CardAdded += OnDrawCardAdded;
        }

        public void Unsubscribe()
        {
            _combatState.Hand.CardAdded -= OnHandCardAdded;
            _combatState.DiscardPile.CardAdded -= OnDiscardCardAdded;
            _combatState.ExhaustPile.CardAdded -= OnExhaustCardAdded;
            _combatState.DrawPile.CardAdded -= OnDrawCardAdded;
        }

        public void OnTurnStarted()
        {
            _endOfTurnDiscardAnnounced = false;
        }

        public void OnShuffleStarted()
        {
            _isShuffling = true;
        }

        public void OnShuffleFinished()
        {
            _isShuffling = false;
            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.DeckShuffled));
        }

        private void OnHandCardAdded(CardModel card)
        {
            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Drew, card.Title));
        }

        private void OnDiscardCardAdded(CardModel card)
        {
            if (CombatManager.Instance.EndingPlayerTurnPhaseTwo
                && !CombatManager.Instance.IsEnemyTurnStarted)
            {
                if (!_endOfTurnDiscardAnnounced)
                {
                    _endOfTurnDiscardAnnounced = true;
                    EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.HandDiscarded));
                }
                return;
            }

            if (RunManager.Instance.ActionExecutor.CurrentlyRunningAction is PlayCardAction pca
                && pca.NetCombatCard.ToCardModelOrNull() == card)
                return;

            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Discarded, card.Title));
        }

        private void OnExhaustCardAdded(CardModel card)
        {
            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Exhausted, card.Title));
        }

        private void OnDrawCardAdded(CardModel card)
        {
            if (_isShuffling) return;
            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.AddedToDraw, card.Title));
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
            EventDispatcher.Enqueue(new BlockEvent(_creature, oldBlock, newBlock));
        }

        public void OnCurrentHpChanged(int oldHp, int newHp)
        {
            EventDispatcher.Enqueue(new HpEvent(_creature, oldHp, newHp));
        }

        public void OnPowerIncreased(PowerModel power, int change, bool silent)
        {
            if (!silent) EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Increased, change));
        }

        public void OnPowerDecreased(PowerModel power, bool silent)
        {
            if (!silent) EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Decreased));
        }

        public void OnPowerRemoved(PowerModel power)
        {
            EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Removed));
        }

        public void OnDied(Creature c)
        {
            EventDispatcher.Enqueue(new DeathEvent(c));
        }
    }
}
