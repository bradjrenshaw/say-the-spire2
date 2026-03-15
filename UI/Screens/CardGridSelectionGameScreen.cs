using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardGridSelectionGameScreen : GameScreen
{
    public static CardGridSelectionGameScreen? Current { get; private set; }

    private readonly Control _screen;
    private readonly NCardGrid _grid;
    private readonly string _containerLabel;

    private static readonly FieldInfo? CardRowsField =
        typeof(NCardGrid).GetField("_cardRows", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

    private static readonly PropertyInfo? ColumnsProperty =
        typeof(NCardGrid).GetProperty("Columns", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

    public override string? ScreenName => _containerLabel;

    public CardGridSelectionGameScreen(NCardGridSelectionScreen screen)
    {
        _screen = screen;
        _grid = screen.GetNode<NCardGrid>("%CardGrid");
        _containerLabel = GetLabel(screen);
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
        Log.Info($"[AccessibilityMod] CardGridSelectionGameScreen built: {gridContainer.Children.Count} cards in grid");
    }

    private static string GetLabel(NCardGridSelectionScreen screen)
    {
        // Try to read the prompt from _prefs.Prompt on the concrete type
        try
        {
            var prefsField = screen.GetType().GetField("_prefs",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (prefsField != null)
            {
                var prefs = prefsField.GetValue(screen);
                var promptProp = prefs?.GetType().GetField("Prompt");
                if (promptProp != null)
                {
                    var prompt = promptProp.GetValue(prefs);
                    var getText = prompt?.GetType().GetMethod("GetFormattedText",
                        BindingFlags.Instance | BindingFlags.Public, null, System.Type.EmptyTypes, null);
                    if (getText != null)
                    {
                        var text = getText.Invoke(prompt, null) as string;
                        if (!string.IsNullOrEmpty(text))
                            return text;
                    }
                }
            }
        }
        catch { }

        return "Card Selection";
    }
}
