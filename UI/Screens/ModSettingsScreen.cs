using Godot;
using SayTheSpire2.Input;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class ModSettingsScreen : Screen
{
    private readonly CategorySetting _category;
    private readonly VBoxContainer _container;
    private readonly NavigableContainer _navContainer;

    public override string? ScreenName => _category.Label;

    public ModSettingsScreen(CategorySetting category)
    {
        _category = category;
        _container = new VBoxContainer { Name = "ModSettings_" + category.Key };
        _navContainer = new NavigableContainer
        {
            ContainerLabel = category.Label,
            AnnounceName = true,
            AnnouncePosition = true,
        };
        RootElement = _navContainer;

        ClaimAction("ui_up");
        ClaimAction("ui_down");
        ClaimAction("ui_accept");
        ClaimAction("ui_select");
        ClaimAction("ui_cancel");

        BuildControls();
    }

    public override void OnPush()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(_container);
        _navContainer.FocusFirst();
    }

    public override void OnFocus()
    {
        _navContainer.FocusFirst();
    }

    public override void OnPop()
    {
        if (GodotObject.IsInstanceValid(_container))
        {
            _container.GetParent()?.RemoveChild(_container);
            _container.QueueFree();
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

    private void BuildControls()
    {
        foreach (var setting in _category.Children)
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
                    _container.AddChild(button.Node);
                    break;

                case BoolSetting boolSetting:
                    var checkbox = new CheckboxElement(boolSetting);
                    _navContainer.Add(checkbox);
                    _container.AddChild(checkbox.Node);
                    break;
            }
        }
    }
}
