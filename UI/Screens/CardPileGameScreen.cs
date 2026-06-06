using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardPileGameScreen : GameScreen
{
    public static CardPileGameScreen? Current { get; private set; }

    private readonly Control _screen;
    private readonly NCardGrid _grid;
    private readonly string _containerLabel;
    private readonly int _cardCount;


    public override Message? ScreenName => Message.Raw(_containerLabel);

    public CardPileGameScreen(NCardPileScreen screen)
    {
        _screen = screen;
        _grid = screen.GetNode<NCardGrid>("CardGrid");
        _containerLabel = GetPileLabel(screen.Pile.Type);
        _cardCount = screen.Pile.Cards.Count;
    }

    public CardPileGameScreen(NDeckViewScreen screen)
    {
        _screen = screen;
        _grid = screen.GetNode<NCardGrid>("CardGrid");
        _containerLabel = new LocString("gameplay_ui", "DECK_PILE_INFO").GetFormattedText();
        try
        {
            var me = MegaCrit.Sts2.Core.Context.LocalContext.GetMe(MegaCrit.Sts2.Core.Runs.RunManager.Instance.DebugOnlyGetState());
            _cardCount = me?.Deck?.Cards?.Count ?? 0;
        }
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Deck count access failed: {e.Message}"); }
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

        var cardRows = CardGridReflection.CardRowsField?.GetValue(_grid) as System.Collections.IList;
        int columns = 1;
        try { columns = CardGridReflection.GetColumns(_grid); }
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Card pile columns access failed: {e.Message}"); }

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

        gridContainer.ContainerLabel = Message.Raw($"{_containerLabel} ({_cardCount})");
        RootElement = gridContainer;
        Log.Info($"[AccessibilityMod] CardPileGameScreen built: {gridContainer.Children.Count} cards in grid");
    }

    private static string GetPileLabel(PileType pileType)
    {
        return pileType switch
        {
            PileType.Draw => PileTitle("DRAW_PILE.title"),
            PileType.Discard => PileTitle("DISCARD_PILE.title"),
            PileType.Exhaust => PileTitle("EXHAUST_PILE.title"),
            _ => pileType.ToString(),
        };
    }

    /// <summary>
    /// The pile title LocStrings include a <c>{Hotkey:choose(None):| ({})}</c>
    /// template that NCombatCardPile resolves by calling <c>Add("Hotkey", …)</c>
    /// with the in-game shortcut key before formatting. Without that, the raw
    /// template ends up in the string (e.g. "Draw Pile {Hotkey:choose(None):|
    /// ({})}"). The mod has its own hotkey bindings and surfaces them through
    /// the help system; we don't want the game's hint baked into the title.
    /// Pass <c>"None"</c> so the template resolves to just the base name.
    /// </summary>
    private static string PileTitle(string key)
    {
        var ls = new LocString("static_hover_tips", key);
        ls.Add("Hotkey", "None");
        return ls.GetFormattedText();
    }
}
