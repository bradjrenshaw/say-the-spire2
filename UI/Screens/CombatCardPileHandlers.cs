using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// Handles card pile events (draw, discard, exhaust, shuffle) for combat.
/// Extracted from CombatScreen for clarity.
/// </summary>
internal class CombatCardPileHandlers
{
    private readonly PlayerCombatState _combatState;
    private bool _isShuffling;
    private Task? _shuffleTask;
    private bool _endOfTurnDiscardAnnounced;

    /// <summary>
    /// Cards that were actively discarded by the player (via CardCmd.Discard).
    /// When these arrive in OnDiscardCardAdded, they're announced as "discarded"
    /// rather than "added to discard pile".
    /// </summary>
    private static readonly HashSet<CardModel> _activelyDiscarded = new();

    public CombatCardPileHandlers(PlayerCombatState combatState)
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
            EventDispatcher.Enqueue(new DeckShuffledEvent());
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    private void OnHandCardAdded(CardModel card)
    {
        Log.Info($"[EventDebug] CardPile.HandAdded: {card.Title} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new CardDrawnEvent(card.Title));
    }

    /// <summary>
    /// Called from the AfterCardDiscarded Harmony hook. Marks a card as actively
    /// discarded so OnDiscardCardAdded can use the correct message.
    /// </summary>
    public static void OnCardActivelyDiscarded(CardModel card)
    {
        _activelyDiscarded.Add(card);
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
                EventDispatcher.Enqueue(new HandDiscardedEvent());
            }
            _activelyDiscarded.Remove(card);
            return;
        }

        if (RunManager.Instance.ActionExecutor.CurrentlyRunningAction is PlayCardAction pca
            && pca.NetCombatCard.ToCardModelOrNull() == card)
        {
            _activelyDiscarded.Remove(card);
            return;
        }

        bool wasActiveDiscard = _activelyDiscarded.Remove(card);
        if (wasActiveDiscard)
        {
            Log.Info($"[EventDebug] CardPile.Discarded: {card.Title} handler={GetHashCode()}");
            EventDispatcher.Enqueue(new CardDiscardedEvent(card.Title));
        }
        else
        {
            Log.Info($"[EventDebug] CardPile.AddedToDiscard: {card.Title} handler={GetHashCode()}");
            EventDispatcher.Enqueue(new CardAddedToDiscardEvent(card.Title));
        }
    }

    private void OnExhaustCardAdded(CardModel card)
    {
        Log.Info($"[EventDebug] CardPile.Exhausted: {card.Title} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new CardExhaustedEvent(card.Title));
    }

    private void OnDrawCardAdded(CardModel card)
    {
        if (_isShuffling) return;
        Log.Info($"[EventDebug] CardPile.AddedToDraw: {card.Title} handler={GetHashCode()}");
        EventDispatcher.Enqueue(new CardAddedToDrawEvent(card.Title));
    }
}
