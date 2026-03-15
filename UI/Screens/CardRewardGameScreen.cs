using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardRewardGameScreen : GameScreen
{
    public static CardRewardGameScreen? Current { get; private set; }

    private readonly NCardRewardSelectionScreen _screen;

    public override string? ScreenName => "Card Reward";

    public CardRewardGameScreen(NCardRewardSelectionScreen screen)
    {
        _screen = screen;
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    protected override void BuildRegistry()
    {
        var list = new ListContainer
        {
            AnnouncePosition = true,
        };

        var cardRow = _screen.GetNodeOrNull<Control>("UI/CardRow");
        if (cardRow != null)
        {
            foreach (var child in cardRow.GetChildren())
            {
                if (child is NGridCardHolder holder)
                {
                    var proxy = new ProxyCard(holder);
                    list.Add(proxy);
                    Register(holder, proxy);
                }
            }
        }

        RootElement = list;
        Log.Info($"[AccessibilityMod] CardRewardGameScreen built: {list.Children.Count} cards");
    }
}
