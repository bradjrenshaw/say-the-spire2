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

        Control? lastReward = null;
        foreach (var child in container.GetChildren())
        {
            if (child is NRewardButton button && button.Visible)
            {
                var proxy = new ProxyRewardButton(button);
                _root.Add(proxy);
                _elementCache[button] = proxy;
                ConnectFocusSignal(button, proxy);
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
                    lastReward = inner;
                }
            }
        }

        var proceedButton = _screen.GetNodeOrNull<NProceedButton>("ProceedButton");
        if (proceedButton is { Visible: true, IsEnabled: true })
        {
            proceedButton.FocusMode = Control.FocusModeEnum.All;
            var proxy = new ProxyButton(proceedButton);
            _root.Add(proxy);
            _elementCache[proceedButton] = proxy;
            ConnectFocusSignal(proceedButton, proxy);
            if (lastReward != null)
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
