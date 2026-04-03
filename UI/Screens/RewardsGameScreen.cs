using System.Collections.Generic;
using System.Linq;
using Godot;
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
        ContainerLabel = LocalizationManager.GetOrDefault("ui", "SCREENS.REWARDS", "Rewards"),
        AnnounceName = true,
        AnnouncePosition = true,
    };
    private readonly Dictionary<Control, UIElement> _elementCache = new();
    private readonly HashSet<ulong> _connectedControls = new();
    private string? _stateToken;

    public override string? ScreenName => LocalizationManager.GetOrDefault("ui", "SCREENS.REWARDS", "Rewards");

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

        foreach (var child in container.GetChildren())
        {
            if (child is NRewardButton button && button.Visible)
            {
                var proxy = new ProxyRewardButton(button);
                _root.Add(proxy);
                _elementCache[button] = proxy;
                ConnectFocusSignal(button, proxy);
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
                }
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
        return string.Join("|", buttons.Select(b => $"{b.GetInstanceId()}:{b.Visible}"));
    }
}
