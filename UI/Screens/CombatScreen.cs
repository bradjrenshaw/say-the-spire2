using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Screens;

public class CombatScreen : Screen
{
    public static CombatScreen? Current { get; private set; }

    private static readonly string[] _alwaysEnabled = { "events" };
    public override IEnumerable<string> AlwaysEnabledBuffers => _alwaysEnabled;

    private CombatState? _currentState;
    private readonly Dictionary<Creature, CreatureHandlers> _subscribedCreatures = new();
    private CardPileHandlers? _cardPileHandlers;

    public CombatScreen()
    {
        ClaimAction("announce_block");
        ClaimAction("announce_energy");
        ClaimAction("announce_powers");
        ClaimAction("announce_intents");
    }

    private CombatState? GetLiveState()
    {
        return CombatManager.Instance?.DebugOnlyGetState();
    }

    public override void OnPush()
    {
        Current = this;
        Log.Info($"[EventDebug] CombatScreen.OnPush: this={GetHashCode()}");
        CombatManager.Instance.CreaturesChanged += OnCreaturesChanged;
        CombatManager.Instance.TurnStarted += OnTurnStarted;

        var state = GetLiveState();
        if (state != null)
            SubscribeToState(state);

        Log.Info("[AccessibilityMod] CombatScreen pushed.");
    }

    public override void OnPop()
    {
        Log.Info($"[EventDebug] CombatScreen.OnPop: this={GetHashCode()}");
        var cm = CombatManager.Instance;
        if (cm != null)
        {
            cm.CreaturesChanged -= OnCreaturesChanged;
            cm.TurnStarted -= OnTurnStarted;
        }
        UnsubscribeFromState();
        if (Current == this) Current = null;
        Log.Info("[AccessibilityMod] CombatScreen popped.");
    }

    public override void OnUpdate()
    {
        var liveState = GetLiveState();
        if (liveState != null && liveState != _currentState)
        {
            Log.Info($"[EventDebug] CombatScreen: CombatState changed, resubscribing");
            UnsubscribeFromState();
            SubscribeToState(liveState);
        }

    }

    private void SubscribeToState(CombatState state)
    {
        _currentState = state;
        SubscribeToAllCreatures(state);

        var player = GetLocalPlayer();
        if (player?.PlayerCombatState != null)
        {
            _cardPileHandlers = new CardPileHandlers(player.PlayerCombatState);
            _cardPileHandlers.Subscribe();
            Log.Info("[EventDebug] CardPileHandlers subscribed.");
        }
    }

    private void UnsubscribeFromState()
    {
        _cardPileHandlers?.Unsubscribe();
        _cardPileHandlers = null;
        UnsubscribeAll();
        _currentState = null;
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
        SpeechManager.Output(Message.Raw($"{player.Creature.Block} block"));
    }

    private void AnnounceEnergy()
    {
        var player = GetLocalPlayer();
        var combatState = player?.PlayerCombatState;
        if (combatState == null) return;
        SpeechManager.Output(Message.Raw(ResourceHelper.GetResourceString(combatState)));
    }

    private void AnnouncePowers()
    {
        var player = GetLocalPlayer();
        if (player == null) return;

        var powers = player.Creature.Powers;
        if (powers.Count == 0)
        {
            SpeechManager.Output(Message.Raw("No powers"));
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
        SpeechManager.Output(Message.Raw(sb.ToString()));
    }

    private void AnnounceIntents()
    {
        var state = GetLiveState();
        if (state == null) return;

        var allies = state.Allies;

        var sb = new StringBuilder();
        foreach (var enemy in state.Enemies)
        {
            if (!enemy.IsAlive || !enemy.IsMonster) continue;

            if (sb.Length > 0) sb.Append(". ");
            sb.Append(enemy.Name);
            sb.Append(": ");

            var move = enemy.Monster!.NextMove;
            var intentParts = new List<string>();
            foreach (var intent in move.Intents)
            {
                var label = intent.GetIntentLabel(allies, enemy).GetFormattedText();
                if (!string.IsNullOrEmpty(label) && label != "")
                    intentParts.Add($"{intent.IntentType} {label}");
                else
                    intentParts.Add(intent.IntentType.ToString());
            }
            sb.Append(intentParts.Count > 0 ? string.Join(", ", intentParts) : "Unknown");
        }

        if (sb.Length == 0)
        {
            SpeechManager.Output(Message.Raw("No enemies"));
            return;
        }

        SpeechManager.Output(Message.Raw(sb.ToString()));
    }

    private Player? GetLocalPlayer()
    {
        if (!RunManager.Instance.IsInProgress) return null;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return null;
        return LocalContext.GetMe(runState);
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
                .FirstOrDefault(c => c != null && c.Hitbox != null);
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

    public void OnShuffleStarting() => _cardPileHandlers?.OnShuffleStarting();
    public void OnShuffleStarted(Task shuffleTask) => _cardPileHandlers?.OnShuffleStarted(shuffleTask);

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

        Log.Info($"[EventDebug] Subscribed to creature: {creature.Name} (alive={creature.IsAlive}, total subs={_subscribedCreatures.Count})");
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
        private Task? _shuffleTask;
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

        public void OnShuffleStarting()
        {
            _isShuffling = true;
        }

        public void OnShuffleStarted(Task shuffleTask)
        {
            _shuffleTask = shuffleTask;
            shuffleTask.ContinueWith(_ =>
            {
                _isShuffling = false;
                _shuffleTask = null;
                EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.DeckShuffled));
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void OnHandCardAdded(CardModel card)
        {
            Log.Info($"[EventDebug] CardPile.HandAdded: {card.Title} handler={GetHashCode()}");
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
                    Log.Info($"[EventDebug] CardPile.HandDiscarded handler={GetHashCode()}");
                    EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.HandDiscarded));
                }
                return;
            }

            if (RunManager.Instance.ActionExecutor.CurrentlyRunningAction is PlayCardAction pca
                && pca.NetCombatCard.ToCardModelOrNull() == card)
                return;

            Log.Info($"[EventDebug] CardPile.Discarded: {card.Title} handler={GetHashCode()}");
            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Discarded, card.Title));
        }

        private void OnExhaustCardAdded(CardModel card)
        {
            Log.Info($"[EventDebug] CardPile.Exhausted: {card.Title} handler={GetHashCode()}");
            EventDispatcher.Enqueue(new CardPileEvent(CardPileEventType.Exhausted, card.Title));
        }

        private void OnDrawCardAdded(CardModel card)
        {
            if (_isShuffling) return;
            Log.Info($"[EventDebug] CardPile.AddedToDraw: {card.Title} handler={GetHashCode()}");
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
            Log.Info($"[EventDebug] CreatureHandler.PowerDecreased: {_creature.Name} {power.Title.GetFormattedText()} silent={silent} handler={GetHashCode()}");
            if (!silent) EventDispatcher.Enqueue(new PowerEvent(_creature, power, PowerEventType.Decreased));
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
}
