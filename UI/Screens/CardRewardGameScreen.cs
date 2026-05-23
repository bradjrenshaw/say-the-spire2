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
    private bool _pendingForceLeftFocus;
    private List<Control> _leftToRightOptions = new();

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.CARD_REWARD");

    public CardRewardGameScreen(NCardRewardSelectionScreen screen)
    {
        _screen = screen;
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
        // Beta-2026-05 regression: the game auto-focuses the middle card
        // instead of the left one when controller is in use. Queue a one-shot
        // override that fires on the next OnUpdate, after the game's own
        // _Ready has had a frame to set its focus. Only runs while the
        // CardReward UI enhancement is enabled.
        _pendingForceLeftFocus = true;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
        _pendingForceLeftFocus = false;
    }

    public override void OnUpdate()
    {
        if (_pendingForceLeftFocus && Settings.UIEnhancementsSettings.CardReward.Get())
        {
            _pendingForceLeftFocus = false;
            ForceLeftmostFocus();
        }
    }

    public void RefreshFromGame(NCardRewardSelectionScreen screen)
    {
        if (screen != _screen)
            return;

        BuildRegistry();
    }

    private void ForceLeftmostFocus()
    {
        if (_leftToRightOptions.Count == 0) return;
        var target = _leftToRightOptions[0];
        if (target == null || !GodotObject.IsInstanceValid(target) || !target.Visible) return;
        try { target.GrabFocus(); }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] CardRewardGameScreen leftmost-focus override failed: {e.Message}");
        }
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
        // Capture the card-only list before alternatives are appended — the
        // left-focus override should land on the leftmost card, not on a
        // Skip/Reroll button if those happen to come first.
        _leftToRightOptions = new List<Control>(optionControls);

        var alternatives = GetAlternatives();
        var alternativesIncluded = Settings.UIEnhancementsSettings.CardRewardAlternatives.Get();
        if (alternativesIncluded)
            optionControls.AddRange(RegisterAlternatives(options, alternatives));

        if (options.Children.Count > 0)
            root.Add(options);

        if (Settings.UIEnhancementsSettings.CardReward.Get())
            WireFocusNeighbors(optionControls);

        RootElement = root;
        Log.Info($"[AccessibilityMod] CardRewardGameScreen built: {optionControls.Count} row options, {alternatives.Count} alternatives ({(alternativesIncluded ? "included" : "excluded")})");
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
