using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Screens;

public class RunScreen : Screen
{
    public static RunScreen? Current { get; private set; }

    private static readonly string[] _alwaysEnabled = { "player" };
    public override System.Collections.Generic.IEnumerable<string> AlwaysEnabledBuffers => _alwaysEnabled;

    private readonly RunState _runState;

    public RunScreen(RunState runState)
    {
        _runState = runState;
        ClaimAction("announce_gold");
        ClaimAction("announce_hp");
    }

    public override void OnPush()
    {
        Current = this;
    }

    public override void OnPop()
    {
        Current = null;
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

    private Player? GetLocalPlayer()
    {
        return LocalContext.GetMe(_runState);
    }
}
