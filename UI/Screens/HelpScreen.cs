using System.Collections.Generic;
using System.Linq;
using Godot;
using SayTheSpire2.Help;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class HelpScreen : Screen
{
    private readonly PanelContainer _root;
    private readonly NavigableContainer _navContainer;
    private readonly Dictionary<ActionElement, DetailState> _detailStates = new();
    private bool _removing;

    public override string? ScreenName => LocalizationManager.GetOrDefault("ui", "SCREENS.HELP", "Help");

    public HelpScreen(List<HelpMessage> messages)
    {
        _root = new PanelContainer { Name = "HelpScreen" };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
        };
        _root.AddThemeStyleboxOverride("panel", bg);

        var centerContainer = new CenterContainer();
        _root.AddChild(centerContainer);

        var contentPanel = new PanelContainer();
        var contentBg = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.15f, 0.2f, 1f),
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 32,
            ContentMarginRight = 32,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        };
        contentPanel.AddThemeStyleboxOverride("panel", contentBg);
        contentPanel.CustomMinimumSize = new Vector2(600, 0);
        centerContainer.AddChild(contentPanel);

        var outerVBox = new VBoxContainer();
        outerVBox.AddThemeConstantOverride("separation", 16);
        contentPanel.AddChild(outerVBox);

        var title = new Label
        {
            Text = LocalizationManager.GetOrDefault("ui", "HELP.TITLE", "Help"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        outerVBox.AddChild(title);
        outerVBox.AddChild(new HSeparator());

        var itemList = new VBoxContainer();
        itemList.AddThemeConstantOverride("separation", 4);
        outerVBox.AddChild(itemList);

        _navContainer = new NavigableContainer
        {
            ContainerLabel = LocalizationManager.GetOrDefault("ui", "CONTAINERS.HELP", "Help"),
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _navContainer;

        foreach (var message in messages)
        {
            ActionElement element;
            switch (message)
            {
                case TextHelpMessage text:
                    element = new ActionElement(() => text.Text);
                    break;
                case ControlHelpMessage control:
                    element = BuildControlElement(control);
                    break;
                default:
                    continue;
            }

            _navContainer.Add(element);
            AddLabel(itemList, element);
        }

        ClaimAllActions();
    }

    private ActionElement BuildControlElement(ControlHelpMessage control)
    {
        if (control.ActionKeys.Count <= 1)
        {
            var keys = control.ActionKeys;
            var desc = control.Description;
            return new ActionElement(
                () => desc,
                status: () => HelpScreenBuilder.FormatBindings(keys) ?? LocalizationManager.GetOrDefault("ui", "HELP.UNBOUND", "unbound"));
        }

        var state = new DetailState(control);
        var element = new ActionElement(
            () => state.GetLabel(),
            status: () => state.GetStatus());
        _detailStates[element] = state;
        return element;
    }

    public override void OnPush()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(_root);
        _navContainer.FocusFirst();
    }

    public override void OnFocus()
    {
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = true;
        _navContainer.FocusFirst();
    }

    public override void OnUnfocus()
    {
        if (!_removing)
        {
            _removing = true;
            ScreenManager.RemoveScreen(this);
        }
    }

    public override void OnPop()
    {
        if (GodotObject.IsInstanceValid(_root))
        {
            _root.GetParent()?.RemoveChild(_root);
            _root.QueueFree();
        }
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (action.Key is "ui_cancel" or "mega_pause_and_back" or "help")
        {
            Close();
            return true;
        }

        if (action.Key is "ui_left" or "ui_right")
        {
            if (TryMoveDetail(action.Key == "ui_right"))
                return true;
        }

        // Reset detail index when navigating up/down
        if (action.Key is "ui_up" or "ui_down")
            ResetFocusedDetail();

        return _navContainer.HandleAction(action);
    }

    private bool TryMoveDetail(bool forward)
    {
        var focused = _navContainer.FocusedChild as ActionElement;
        if (focused == null || !_detailStates.TryGetValue(focused, out var state))
            return false;

        var moved = forward ? state.MoveNext() : state.MovePrevious();
        if (!moved)
            return true; // consume the action even if at boundary

        var label = state.GetLabel();
        var status = state.GetStatus();
        SpeechManager.Output(Message.Raw($"{label}, {status}"));
        return true;
    }

    private void ResetFocusedDetail()
    {
        var focused = _navContainer.FocusedChild as ActionElement;
        if (focused != null && _detailStates.TryGetValue(focused, out var state))
            state.Reset();
    }

    private void Close()
    {
        _removing = true;
        ScreenManager.RemoveScreen(this);
        SpeechManager.Output(LocalizationManager.GetOrDefault("ui", "SPEECH.CLOSED", "Closed"));
    }

    private static void AddLabel(VBoxContainer list, ActionElement element)
    {
        var label = new Label
        {
            Text = element.GetLabel()?.Resolve() ?? "",
            AutowrapMode = TextServer.AutowrapMode.Word,
        };
        label.AddThemeFontSizeOverride("font_size", 16);
        list.AddChild(label);
    }

    private class DetailState
    {
        private readonly ControlHelpMessage _control;
        private int _index = -1; // -1 = overview

        public DetailState(ControlHelpMessage control)
        {
            _control = control;
        }

        public string GetLabel()
        {
            if (_index < 0)
                return _control.Description;

            var actionKey = _control.ActionKeys[_index];
            var action = InputManager.Actions.FirstOrDefault(a => a.Key == actionKey);
            return action?.Label ?? _control.Description;
        }

        public string GetStatus()
        {
            if (_index < 0)
                return HelpScreenBuilder.FormatBindings(_control.ActionKeys) ?? LocalizationManager.GetOrDefault("ui", "HELP.UNBOUND", "unbound");

            return HelpScreenBuilder.FormatBindings(new[] { _control.ActionKeys[_index] }) ?? LocalizationManager.GetOrDefault("ui", "HELP.UNBOUND", "unbound");
        }

        public bool MoveNext()
        {
            if (_index >= _control.ActionKeys.Count - 1)
                return false;
            _index++;
            return true;
        }

        public bool MovePrevious()
        {
            if (_index <= -1)
                return false;
            _index--;
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }
    }
}
