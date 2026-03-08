using Godot;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Elements;

public class DropdownElement : UIElement
{
    private readonly Button _control;
    private readonly ChoiceSetting _setting;

    public Node Node => _control;
    public ChoiceSetting Setting => _setting;

    public DropdownElement(ChoiceSetting setting)
    {
        _setting = setting;
        _control = new Button
        {
            Text = GetButtonText(),
            FocusMode = Control.FocusModeEnum.None,
        };

        _setting.Changed += _ => _control.Text = GetButtonText();
    }

    public override string? GetLabel() => _setting.Label;
    public override string? GetTypeKey() => "dropdown";

    public override string? GetStatusString()
    {
        var selected = _setting.GetSelected();
        return selected?.Label ?? _setting.Get();
    }

    private string GetButtonText()
    {
        var selected = _setting.GetSelected();
        return $"{_setting.Label}: {selected?.Label ?? _setting.Get()}";
    }
}
