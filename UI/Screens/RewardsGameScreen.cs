using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SayTheSpire2.Help;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class RewardsGameScreen : Screen
{
    public static RewardsGameScreen? Current { get; private set; }

    private readonly NRewardsScreen _screen;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Message.Localized("ui", "SCREENS.REWARDS"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, UIElement> _elementCache = new();
    private readonly HashSet<ulong> _connectedControls = new();
    private string? _stateToken;

    public override Message? ScreenName => Message.Localized("ui", "SCREENS.REWARDS");

    public override List<HelpMessage> GetHelpMessages() => new()
    {
        new TextHelpMessage(
            LocalizationManager.GetOrDefault("ui", "HELP.REWARDS_NAV",
                "Navigate rewards with directional controls. Press Select to claim a reward."),
            exclusive: true),
        new ControlHelpMessage(
            LocalizationManager.GetOrDefault("ui", "HELP.REWARDS_PROCEED",
                "Proceed (skipping any unclaimed rewards)"),
            "ui_accept", exclusive: true),
    };

    public RewardsGameScreen(NRewardsScreen screen)
    {
        _screen = screen;
        RootElement = _root;
    }

    public override void OnPush()
    {
        Current = this;
        _stateToken = BuildStateToken();
        BuildElements();
    }

    public override void OnPop()
    {
        _elementCache.Clear();
        if (Current == this) Current = null;
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_screen))
        {
            ScreenManager.RemoveScreen(this);
            return;
        }

        var token = BuildStateToken();
        if (token != _stateToken)
        {
            _stateToken = token;
            BuildElements();
        }
    }

    public override UIElement? GetElement(Control control)
    {
        return _elementCache.TryGetValue(control, out var element) ? element : null;
    }

    private void BuildElements()
    {
        _root.Clear();
        _elementCache.Clear();

        var container = _screen.GetNodeOrNull<Control>("%RewardsContainer");
        if (container == null) return;

        Control? firstReward = null;
        Control? lastReward = null;
        foreach (var child in container.GetChildren())
        {
            if (child is NRewardButton button && button.Visible)
            {
                var proxy = new ProxyRewardButton(button);
                _root.Add(proxy);
                _elementCache[button] = proxy;
                ConnectFocusSignal(button, proxy);
                firstReward ??= button;
                lastReward = button;
            }
            else if (child is Control control && control.Visible)
            {
                // NLinkedRewardSet or other controls — find NRewardButtons inside
                foreach (var inner in control.GetChildren().OfType<NRewardButton>().Where(b => b.Visible))
                {
                    var proxy = new ProxyRewardButton(inner);
                    _root.Add(proxy);
                    _elementCache[inner] = proxy;
                    ConnectFocusSignal(inner, proxy);
                    firstReward ??= inner;
                    lastReward = inner;
                }
            }
        }

        Control? focusableProceed = null;
        var proceedButton = _screen.GetNodeOrNull<NProceedButton>("ProceedButton");
        if (proceedButton is { Visible: true, IsEnabled: true })
        {
            focusableProceed = proceedButton;
            proceedButton.FocusMode = Control.FocusModeEnum.All;
            var proxy = new ProxyButton(proceedButton);
            _root.Add(proxy);
            _elementCache[proceedButton] = proxy;
            ConnectFocusSignal(proceedButton, proxy);
            if (lastReward != null && Settings.UIEnhancementsSettings.Rewards.Get())
            {
                var proceedPath = proceedButton.GetPath();
                var rewardPath = lastReward.GetPath();
                lastReward.FocusNeighborBottom = proceedPath;
                proceedButton.FocusNeighborTop = rewardPath;
                proceedButton.FocusNeighborBottom = proceedPath;
                proceedButton.FocusNeighborLeft = proceedPath;
                proceedButton.FocusNeighborRight = proceedPath;
            }
        }

        // Relics wire down to the first reward, or to the proceed/skip button
        // when no rewards remain (all claimed) but it's still focusable via
        // our enhancement. Done after the proceed button is resolved so the
        // fallback target is known.
        WireRelicsToRewards(firstReward ?? focusableProceed);
    }

    /// <summary>
    /// Point each top-bar relic's down-neighbor at <paramref name="target"/>
    /// (the first reward button, or the proceed/skip button when no rewards
    /// remain) so navigating up to relics and back down lands on the rewards
    /// menu. The game (and our out-of-combat wiring in RunScreen) deliberately
    /// leaves relic FocusNeighborBottom for the active screen to set — for
    /// most screens the game's content fills that role, but the rewards
    /// overlay doesn't, and after combat the relic-bottom can still point at a
    /// now-gone creature. This is scoped to the rewards screen only, so
    /// RunScreen's general "leave relic-bottom to the game" behavior (and the
    /// multiplayer cases that depend on it) is untouched.
    /// </summary>
    private void WireRelicsToRewards(Control? target)
    {
        if (target == null || !Settings.UIEnhancementsSettings.Rewards.Get())
            return;

        var relicNodes = MegaCrit.Sts2.Core.Nodes.NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes;
        if (relicNodes == null) return;

        var targetPath = target.GetPath();
        foreach (var relic in relicNodes)
        {
            if (relic == null || !GodotObject.IsInstanceValid(relic)) continue;
            relic.FocusNeighborBottom = targetPath;
        }
    }

    private void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;

        control.FocusEntered += () => UIManager.SetFocusedControl(control, element);
    }

    private string BuildStateToken()
    {
        var container = _screen.GetNodeOrNull<Control>("%RewardsContainer");
        if (container == null) return "";
        var buttons = container.GetChildren().OfType<Control>().Where(c => c.Visible);
        var proceedButton = _screen.GetNodeOrNull<NProceedButton>("ProceedButton");
        var proceedToken = proceedButton == null
            ? "proceed:null"
            : $"proceed:{proceedButton.GetInstanceId()}:{proceedButton.Visible}:{proceedButton.IsEnabled}:{proceedButton.IsSkip}:{proceedButton.FocusMode}";
        return string.Join("|", buttons.Select(b => $"{b.GetInstanceId()}:{b.Visible}")) + "|" + proceedToken;
    }
}
