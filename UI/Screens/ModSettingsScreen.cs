using System.Linq;
using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ModSettingsScreen : Screen
{
    private readonly CategorySetting _category;
    private readonly PanelContainer _root;
    private readonly VBoxContainer _itemList;
    private readonly NavigableContainer _navContainer;

    public override string? ScreenName => _category.Label;

    public ModSettingsScreen(CategorySetting category)
    {
        _category = category;

        // Build visual layout
        _root = new PanelContainer { Name = "ModSettings_" + category.Key };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Semi-transparent dark background
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
        };
        _root.AddThemeStyleboxOverride("panel", bg);

        // Centered content area
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

        // Title
        var title = new Label
        {
            Text = category.IsRoot ? LocalizationManager.GetOrDefault("ui", "SCREENS.MOD_SETTINGS", "Mod Settings") : category.Label,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        outerVBox.AddChild(title);

        // Separator
        outerVBox.AddChild(new HSeparator());

        // Item list
        _itemList = new VBoxContainer();
        _itemList.AddThemeConstantOverride("separation", 8);
        outerVBox.AddChild(_itemList);

        // Navigation container
        _navContainer = new NavigableContainer
        {
            ContainerLabel = category.Label,
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
        ClaimAction("mega_pause_and_back");
        ClaimAction("mod_settings");

        BuildControls();
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

        if (_navContainer.FocusIndex >= 0)
            _navContainer.SetFocusIndex(_navContainer.FocusIndex);
        else
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
        if (action.Key == "ui_cancel" || action.Key == "mod_settings")
        {
            ScreenManager.RemoveScreen(this);
            if (_category.IsRoot)
                SpeechManager.Output(Message.Localized("ui", "SPEECH.CLOSED"));
            return true;
        }

        return _navContainer.HandleAction(action);
    }

    private void BuildControls()
    {
        foreach (var setting in _category.Children.OrderBy(s => s.SortPriority).ThenBy(s => s.Label))
        {
            switch (setting)
            {
                case CategorySetting cat:
                    var button = new ButtonElement(cat.Label);
                    button.OnActivated = () =>
                    {
                        var subScreen = new ModSettingsScreen(cat);
                        ScreenManager.PushScreen(subScreen);
                    };
                    _navContainer.Add(button);
                    AddControl(button.Node, button);
                    break;

                case BoolSetting boolSetting:
                    var checkbox = new CheckboxElement(boolSetting);
                    _navContainer.Add(checkbox);
                    AddControl(checkbox.Node, checkbox);
                    break;

                case IntSetting intSetting:
                    var slider = new SliderElement(intSetting);
                    _navContainer.Add(slider);
                    AddControl(slider.Node, slider);
                    break;

                case ChoiceSetting choiceSetting:
                    var dropdown = new DropdownElement(choiceSetting);
                    _navContainer.Add(dropdown);
                    AddControl(dropdown.Node, dropdown);
                    break;

                case BindingSetting bindingSetting:
                    var bindingLabel = GetBindingSummary(bindingSetting);
                    var bindingButton = new ButtonElement(bindingLabel);
                    bindingButton.OnActivated = () =>
                    {
                        var screen = new BindingListScreen(bindingSetting);
                        ScreenManager.PushScreen(screen);
                    };
                    _navContainer.Add(bindingButton);
                    AddControl(bindingButton.Node, bindingButton);
                    break;
            }
        }
    }

    private static string GetBindingSummary(BindingSetting setting)
    {
        var action = setting.Action;
        var bindings = action.Bindings;
        if (bindings.Count == 0)
            return $"{action.Label}: (none)";
        var names = string.Join(", ", bindings.Select(b => b.DisplayName));
        return $"{action.Label}: {names}";
    }

    private void AddControl(Node node, UIElement element)
    {
        var control = (Control)node;
        control.FocusMode = Control.FocusModeEnum.All;
        _itemList.AddChild(control);

        // Sync keyboard navigation when mouse clicks a control
        control.FocusEntered += () =>
        {
            _navContainer.SetFocusTo(element);
        };

        // Handle mouse activation
        if (element is ButtonElement btn)
        {
            ((BaseButton)control).Pressed += () => btn.Activate();
        }
        else if (element is CheckboxElement cb)
        {
            ((CheckBox)control).Toggled += (_) => cb.SyncFromControl();
        }
        else if (element is SliderElement sl)
        {
            ((HSlider)control).ValueChanged += (_) => sl.SyncFromControl();
        }
        else if (element is DropdownElement dd)
        {
            ((BaseButton)control).Pressed += () =>
            {
                var screen = new ChoiceSelectionScreen(dd.Setting);
                ScreenManager.PushScreen(screen);
            };
        }
    }
}
