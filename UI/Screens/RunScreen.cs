using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Screens;

public class RunScreen : Screen
{
    public static RunScreen? Current { get; private set; }

    private static readonly string[] _alwaysEnabled = { "player" };
    public override System.Collections.Generic.IEnumerable<string> AlwaysEnabledBuffers => _alwaysEnabled;

    private Player? _subscribedPlayer;

    public RunScreen()
    {
        ClaimAction("announce_gold");
        ClaimAction("announce_hp");
        ClaimAction("announce_boss");
    }

    public override void OnPush()
    {
        Current = this;
        SubscribeToPlayer();
    }

    public override void OnPop()
    {
        UnsubscribeFromPlayer();
        Current = null;
    }

    public override void OnUpdate()
    {
        var currentPlayer = GetLocalPlayer();
        if (currentPlayer != null && currentPlayer != _subscribedPlayer)
        {
            UnsubscribeFromPlayer();
            SubscribeToPlayer();
        }
    }

    private void SubscribeToPlayer()
    {
        _subscribedPlayer = GetLocalPlayer();
        if (_subscribedPlayer != null)
        {
            _subscribedPlayer.Deck.CardAdded += OnCardObtained;
            _subscribedPlayer.RelicObtained += OnRelicObtained;
            _subscribedPlayer.PotionProcured += OnPotionObtained;
        }
    }

    private void UnsubscribeFromPlayer()
    {
        if (_subscribedPlayer != null)
        {
            _subscribedPlayer.Deck.CardAdded -= OnCardObtained;
            _subscribedPlayer.RelicObtained -= OnRelicObtained;
            _subscribedPlayer.PotionProcured -= OnPotionObtained;
            _subscribedPlayer = null;
        }
    }

    private void OnCardObtained(CardModel card)
    {
        var name = card.Title;
        if (!string.IsNullOrEmpty(name))
            EventDispatcher.Enqueue(new CardObtainedEvent(name));
    }

    private void OnRelicObtained(RelicModel relic)
    {
        var name = relic.Title.GetFormattedText();
        if (!string.IsNullOrEmpty(name))
            EventDispatcher.Enqueue(new RelicObtainedEvent(name));
    }

    private void OnPotionObtained(PotionModel potion)
    {
        var name = potion.Title.GetFormattedText();
        if (!string.IsNullOrEmpty(name))
            EventDispatcher.Enqueue(new PotionObtainedEvent(name));
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "announce_gold":
                AnnounceGold();
                return true;
            case "announce_hp":
                AnnounceHp();
                return true;
            case "announce_boss":
                AnnounceBoss();
                return true;
        }

        return false;
    }

    private void AnnounceGold()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        SpeechManager.Output(Message.Raw($"{player.Gold} gold"));
    }

    private void AnnounceHp()
    {
        var player = GetLocalPlayer();
        if (player == null) return;
        SpeechManager.Output(Message.Raw($"{player.Creature.CurrentHp} of {player.Creature.MaxHp} HP"));
    }

    private void AnnounceBoss()
    {
        if (!RunManager.Instance.IsInProgress) return;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return;

        var boss = runState.Act.BossEncounter;
        var name = boss.Title.GetFormattedText();

        if (runState.Act.HasSecondBoss)
        {
            var second = runState.Act.SecondBossEncounter;
            var secondName = second?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(secondName))
                name = $"{name} and {secondName}";
        }

        SpeechManager.Output(Message.Raw(name));
    }

    private Player? GetLocalPlayer()
    {
        if (!RunManager.Instance.IsInProgress) return null;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return null;
        return LocalContext.GetMe(runState);
    }
}
