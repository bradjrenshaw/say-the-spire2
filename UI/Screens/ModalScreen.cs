using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using SayTheSpire2.Help;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ModalScreen : Screen
{
    public static ModalScreen? Current { get; private set; }

    private readonly Node _modal;
    private readonly Dictionary<Control, UIElement> _elementCache = new();
    private readonly HashSet<ulong> _connectedControls = new();
    private readonly ListContainer _root = new()
    {
        ContainerLabel = LocalizationManager.GetOrDefault("ui", "SCREENS.DIALOG", "Dialog"),
        AnnounceName = true,
        AnnouncePosition = true,
    };

    public override string? ScreenName => null; // Modal text is announced separately by ModalHooks

    public override List<HelpMessage> GetHelpMessages()
    {
        if (_modal is NCombatRulesFtue)
        {
            return new()
            {
                new TextHelpMessage(
                    LocalizationManager.GetOrDefault("ui", "HELP.TUTORIAL_NAV",
                        "Use left and right to navigate between pages. Press Confirm to close."),
                    exclusive: true),
                new ControlHelpMessage(
                    LocalizationManager.GetOrDefault("ui", "HELP.PREVIOUS_PAGE", "Previous Page"),
                    "ui_left", exclusive: true),
                new ControlHelpMessage(
                    LocalizationManager.GetOrDefault("ui", "HELP.NEXT_PAGE", "Next Page"),
                    "ui_right", exclusive: true),
            };
        }

        return new()
        {
            new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.CONFIRM", "Confirm"), "ui_select", exclusive: true),
            new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.CANCEL_GO_BACK", "Cancel / Go Back"), new[] { "ui_cancel", "mega_pause_and_back" }, exclusive: true),
        };
    }

    public ModalScreen(Node modal)
    {
        _modal = modal;
        RootElement = _root;
    }

    public override void OnPush()
    {
        Current = this;
        BuildElements();
    }

    public override void OnPop()
    {
        _elementCache.Clear();
        if (Current == this) Current = null;
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_modal) || !((Control)_modal).IsVisibleInTree())
        {
            ScreenManager.RemoveScreen(this);
            return;
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

        foreach (var button in FindButtons())
        {
            var proxy = ProxyFactory.Create(button);
            _root.Add(proxy);
            _elementCache[button] = proxy;
            ConnectFocusSignal(button, proxy);
        }
    }

    private void ConnectFocusSignal(Control control, UIElement element)
    {
        if (!_connectedControls.Add(control.GetInstanceId()))
            return;

        control.FocusEntered += () => UIManager.SetFocusedControl(control, element);
    }

    private List<NClickableControl> FindButtons()
    {
        var buttons = new List<NClickableControl>();
        FindButtonsRecursive(_modal, buttons);
        return buttons;
    }

    private static void FindButtonsRecursive(Node node, List<NClickableControl> buttons)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is NClickableControl ncc && ncc.Visible)
                buttons.Add(ncc);
            else
                FindButtonsRecursive(child, buttons);
        }
    }

}
