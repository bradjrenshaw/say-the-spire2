using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ModalScreen : Screen
{
    public const string BlitzDisclaimerTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzInfoDisclaimer";
    public const string DreadDisclaimerTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadInfoDisclaimer";

    /// <summary>
    /// True for a game mode's info popup (Blitz, Dread): a child of the mode's
    /// lobby rather than a real modal, with one button on the game's confirm
    /// action.
    /// </summary>
    public static bool IsModeDisclaimer(Node modal) =>
        modal.GetType().FullName is BlitzDisclaimerTypeName or DreadDisclaimerTypeName;

    public static ModalScreen? Current { get; private set; }

    private readonly Node _modal;
    private readonly Dictionary<Control, UIElement> _elementCache = new();
    private readonly HashSet<ulong> _connectedControls = new();
    // Mode info popups only: the popup's one button, which holds focus while
    // the popup is up, and the lobby control focus goes back to afterwards.
    private Control? _trapButton;
    private Control? _returnFocus;
    private readonly ListContainer _root = new()
    {
        ContainerLabel = Message.Localized("ui", "SCREENS.DIALOG"),
        AnnounceName = true,
        AnnouncePosition = true,
    };

    public override Message? ScreenName => null; // Modal text is announced separately by ModalHooks

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

        // The mode info popup has a single button and nothing to cancel.
        // Both of our confirm keys close it (see the constructor).
        if (IsModeDisclaimer(_modal))
        {
            return new()
            {
                new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.CONFIRM", "Confirm"), new[] { "ui_select", "ui_accept" }, exclusive: true),
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

        // A mode info popup doesn't take focus in the game — the lobby behind
        // it stays navigable — but it blocks every game hotkey until its
        // button is pressed, including the lobby's Embark. Without sight
        // that reads as a lobby where Enter on a character silently does
        // nothing. So while one is open the mod treats it as the dialog it
        // looks like: its button holds focus (see TrapFocus), and both of
        // our confirm keys press it — the button is on the game's confirm
        // action, which is Enter on the game's own keyboard layout but only
        // our Accept key here.
        if (IsModeDisclaimer(modal))
        {
            ClaimAction("ui_select");
            ClaimAction("ui_accept");
        }
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (!IsModeDisclaimer(_modal) || action.Key is not ("ui_select" or "ui_accept"))
            return false;
        if (!GodotObject.IsInstanceValid(_modal))
            return false;

        var buttons = FindButtons();
        if (buttons.Count == 0)
            return false;
        buttons[0].ForceClick();
        return true;
    }

    public override void OnPush()
    {
        Current = this;
        BuildElements();
        if (IsModeDisclaimer(_modal))
            TrapFocus();
    }

    public override void OnPop()
    {
        _elementCache.Clear();
        if (Current == this) Current = null;
        ReleaseFocus();
    }

    public override void OnUpdate()
    {
        if (!GodotObject.IsInstanceValid(_modal) || !((Control)_modal).IsVisibleInTree())
        {
            ScreenManager.RemoveScreen(this);
            return;
        }

        HoldFocus();
    }

    // While a mode info popup holds focus, nothing behind it is announced:
    // the game still focuses a character when the lobby opens.
    public override bool ShouldSuppressFocusAnnouncement(Control control) =>
        _trapButton != null && control != _trapButton;

    // Puts focus on the popup's button, so it is read after the popup's text
    // ("Confirm, button") and the arrow keys have nowhere to go. The game
    // never makes that button focusable (its scene sets no focus mode).
    private void TrapFocus()
    {
        var buttons = FindButtons();
        if (buttons.Count == 0) return;

        var button = buttons[0];
        var self = button.GetPath();
        button.FocusMode = Control.FocusModeEnum.All;
        button.FocusNeighborLeft = self;
        button.FocusNeighborRight = self;
        button.FocusNeighborTop = self;
        button.FocusNeighborBottom = self;
        button.FocusNext = self;
        button.FocusPrevious = self;

        _trapButton = button;
        HoldFocus();
    }

    // The game focuses a lobby control whenever it likes (opening the lobby,
    // re-enabling buttons); take focus back and remember where it wanted to be.
    private void HoldFocus()
    {
        if (_trapButton == null || !GodotObject.IsInstanceValid(_trapButton) || !_trapButton.IsVisibleInTree())
            return;

        var owner = _trapButton.GetViewport()?.GuiGetFocusOwner();
        if (owner == _trapButton) return;
        if (owner != null)
            _returnFocus = owner;
        _trapButton.GrabFocus();
    }

    // Popup gone: hand focus back to the lobby, which announces it normally.
    private void ReleaseFocus()
    {
        if (_trapButton == null) return;
        _trapButton = null;

        var target = _returnFocus;
        _returnFocus = null;
        if (target != null && GodotObject.IsInstanceValid(target) && target.IsVisibleInTree()
            && target.FocusMode != Control.FocusModeEnum.None)
            target.GrabFocus();
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
