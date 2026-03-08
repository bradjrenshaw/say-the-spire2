using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ChoiceSelectionScreen : Screen
{
    private readonly ChoiceSetting _setting;
    private readonly PanelContainer _root;
    private readonly VBoxContainer _itemList;
    private readonly NavigableContainer _navContainer;

    public override string? ScreenName => _setting.Label;

    public ChoiceSelectionScreen(ChoiceSetting setting)
    {
        _setting = setting;

        _root = new PanelContainer { Name = "ChoiceSelection_" + setting.Key };
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
        contentPanel.CustomMinimumSize = new Vector2(500, 0);
        centerContainer.AddChild(contentPanel);

        var outerVBox = new VBoxContainer();
        outerVBox.AddThemeConstantOverride("separation", 16);
        contentPanel.AddChild(outerVBox);

        var title = new Label
        {
            Text = setting.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        outerVBox.AddChild(title);
        outerVBox.AddChild(new HSeparator());

        _itemList = new VBoxContainer();
        _itemList.AddThemeConstantOverride("separation", 8);
        outerVBox.AddChild(_itemList);

        _navContainer = new NavigableContainer
        {
            ContainerLabel = setting.Label,
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _navContainer;

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_left");
        ClaimAction("ui_right");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("ui_cancel");

        BuildOptions();
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
        if (GodotObject.IsInstanceValid(_root))
            _root.Visible = false;
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
        if (action.Key == "ui_cancel")
        {
            ScreenManager.RemoveScreen(this);
            SpeechManager.Output("Closed");
            return true;
        }

        return _navContainer.HandleAction(action);
    }

    private void BuildOptions()
    {
        foreach (var choice in _setting.Options)
        {
            var button = new ButtonElement(choice.Label);
            button.OnActivated = () =>
            {
                _setting.Set(choice.Key);
                ScreenManager.RemoveScreen(this);
                SpeechManager.Output($"{choice.Label} selected");
            };
            _navContainer.Add(button);

            var control = (Control)button.Node;
            control.FocusMode = Control.FocusModeEnum.All;
            _itemList.AddChild(control);

            control.FocusEntered += () => _navContainer.SetFocusTo(button);
            ((BaseButton)control).Pressed += () => button.Activate();
        }
    }
}
