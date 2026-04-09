using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardGridSelectionGameScreen : GameScreen
{
    public static CardGridSelectionGameScreen? Current { get; private set; }

    private readonly Control _screen;
    private readonly NCardGrid _grid;
    private readonly string _containerLabel;


    private static readonly FieldInfo? SelectedCardsField =
        AccessTools.Field(typeof(NSimpleCardSelectScreen), "_selectedCards");

    private HashSet<CardModel>? _selectedCards;

    public override string? ScreenName => _containerLabel;

    public CardGridSelectionGameScreen(NCardGridSelectionScreen screen)
    {
        _screen = screen;
        _grid = screen.GetNode<NCardGrid>("%CardGrid");
        _containerLabel = GetLabel(screen);

        if (screen is NSimpleCardSelectScreen)
            _selectedCards = SelectedCardsField?.GetValue(screen) as HashSet<CardModel>;
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
                        if (_selectedCards != null)
                        {
                            var cardHolder = holder;
                            var selectedSet = _selectedCards;
                            proxy.CollectPreExtras += extras =>
                            {
                                var model = cardHolder.CardModel;
                                if (model != null && selectedSet.Contains(model))
                                    extras.Add(Localization.Message.Localized("ui", "CARD.SELECTED"));
                            };
                            proxy.CollectPostExtras += extras =>
                            {
                                extras.Add(Localization.Message.Localized("ui", "CARD.COUNT_SELECTED", new { count = selectedSet.Count }));
                            };
                        }
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
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Card grid selection label access failed: {e.Message}"); }

        return LocalizationManager.GetOrDefault("ui", "LABELS.CARD_SELECTION", "Card Selection");
    }
}
