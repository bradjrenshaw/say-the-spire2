using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardPileGameScreen : GameScreen
{
    public static CardPileGameScreen? Current { get; private set; }

    private readonly Control _screen;
    private readonly NCardGrid _grid;
    private readonly string _containerLabel;

    private static readonly FieldInfo? CardRowsField =
        typeof(NCardGrid).GetField("_cardRows", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

    private static readonly PropertyInfo? ColumnsProperty =
        typeof(NCardGrid).GetProperty("Columns", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

    public override string? ScreenName => _containerLabel;

    public CardPileGameScreen(NCardPileScreen screen)
    {
        _screen = screen;
        _grid = screen.GetNode<NCardGrid>("CardGrid");
        _containerLabel = GetPileLabel(screen.Pile.Type);
    }

    public CardPileGameScreen(NDeckViewScreen screen)
    {
        _screen = screen;
        _grid = screen.GetNode<NCardGrid>("CardGrid");
        _containerLabel = new LocString("gameplay_ui", "DECK_PILE_INFO").GetFormattedText();
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
        var gridContainer = new Elements.GridContainer
        {
            AnnounceName = true,
            AnnouncePosition = true,
        };

        var cardRows = CardRowsField?.GetValue(_grid) as System.Collections.IList;
        int columns = 1;
        try { columns = (int)(ColumnsProperty?.GetValue(_grid) ?? 1); }
        catch { }

        if (cardRows != null)
        {
            for (int row = 0; row < cardRows.Count; row++)
            {
                var rowList = cardRows[row] as System.Collections.IList;
                if (rowList == null) continue;

                for (int col = 0; col < rowList.Count; col++)
                {
                    if (rowList[col] is NGridCardHolder holder)
                    {
                        var proxy = new ProxyCard(holder);
                        gridContainer.Add(proxy, col, row);
                        Register(holder, proxy);
                    }
                }
            }
        }

        gridContainer.ContainerLabel = $"{_containerLabel} ({gridContainer.Children.Count})";
        RootElement = gridContainer;
        Log.Info($"[AccessibilityMod] CardPileGameScreen built: {gridContainer.Children.Count} cards in grid");
    }

    private static string GetPileLabel(PileType pileType)
    {
        return pileType switch
        {
            PileType.Draw => new LocString("static_hover_tips", "DRAW_PILE.title").GetFormattedText(),
            PileType.Discard => new LocString("static_hover_tips", "DISCARD_PILE.title").GetFormattedText(),
            PileType.Exhaust => new LocString("static_hover_tips", "EXHAUST_PILE.title").GetFormattedText(),
            _ => pileType.ToString(),
        };
    }
}
