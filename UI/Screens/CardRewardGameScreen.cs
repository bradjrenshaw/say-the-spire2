using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class CardRewardGameScreen : GameScreen
{
    public static CardRewardGameScreen? Current { get; private set; }

    private readonly NCardRewardSelectionScreen _screen;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.CARD_REWARD");

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

    public void RefreshFromGame(NCardRewardSelectionScreen screen)
    {
        if (screen != _screen)
            return;

        BuildRegistry();
    }

    protected override void BuildRegistry()
    {
        ClearRegistry();
        var root = new ListContainer
        {
            AnnounceName = false,
            AnnouncePosition = false,
        };
        var options = new ListContainer
        {
            AnnounceName = false,
            AnnouncePosition = true,
        };

        var optionControls = RegisterCards(options);
        var alternatives = GetAlternatives();
        optionControls.AddRange(RegisterAlternatives(options, alternatives));

        if (options.Children.Count > 0)
            root.Add(options);

        if (Settings.UIEnhancementsSettings.CardReward.Get())
            WireFocusNeighbors(optionControls);

        RootElement = root;
        Log.Info($"[AccessibilityMod] CardRewardGameScreen built: {optionControls.Count} row options, {alternatives.Count} alternatives");
    }

    private List<Control> RegisterCards(ListContainer container)
    {
        var controls = new List<Control>();
        var cardRow = _screen.GetNodeOrNull<Control>("UI/CardRow");
        if (cardRow == null)
            return controls;

        // Collect holders then reverse — the game focuses the middle/right card
        // first, and visual order is right-to-left in the reward screen
        var holders = new List<NGridCardHolder>();
        foreach (var child in cardRow.GetChildren())
        {
            if (child is NGridCardHolder holder && IsLiveControl(holder))
                holders.Add(holder);
        }
        holders.Reverse();

        foreach (var holder in holders)
        {
            var proxy = new ProxyCard(holder);
            container.Add(proxy);
            Register(holder, proxy);
            controls.Add(holder);
        }

        return controls;
    }

    private List<NCardRewardAlternativeButton> GetAlternatives()
    {
        var buttons = new List<NCardRewardAlternativeButton>();
        var gameContainer = _screen.GetNodeOrNull<Control>("UI/RewardAlternatives");
        if (gameContainer == null)
            return buttons;

        foreach (var child in gameContainer.GetChildren())
        {
            if (child is not NCardRewardAlternativeButton button || !IsLiveControl(button))
                continue;

            buttons.Add(button);
        }

        return buttons;
    }

    private List<Control> RegisterAlternatives(ListContainer row, List<NCardRewardAlternativeButton> alternatives)
    {
        var controls = new List<Control>();
        foreach (var button in alternatives)
        {
            var proxy = new ProxyButton(button);
            row.Add(proxy);
            Register(button, proxy);
            controls.Add(button);
        }

        return controls;
    }

    private static void WireFocusNeighbors(List<Control> options)
    {
        if (options.Count == 0)
            return;

        for (var i = 0; i < options.Count; i++)
        {
            var self = options[i].GetPath();
            var left = i > 0 ? options[i - 1] : options[^1];
            var right = i < options.Count - 1 ? options[i + 1] : options[0];

            options[i].FocusNeighborLeft = left.GetPath();
            options[i].FocusNeighborRight = right.GetPath();
            options[i].FocusNeighborTop = self;
            options[i].FocusNeighborBottom = self;
        }
    }

    private static bool IsLiveControl(Control control)
    {
        // RefreshOptions queues old cards/buttons for deletion, then creates
        // replacements synchronously. Filtering the queued nodes is more precise
        // than waiting a frame and less invasive than patching every factory.
        return IsUsable(control) && !control.IsQueuedForDeletion();
    }
}
