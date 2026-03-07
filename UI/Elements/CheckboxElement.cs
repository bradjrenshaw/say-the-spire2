using Godot;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class CheckboxElement : UIElement
{
    private readonly CheckBox _control;
    private readonly BoolSetting _setting;

    public Node Node => _control;

    public CheckboxElement(BoolSetting setting)
    {
        _setting = setting;
        _control = new CheckBox
        {
            Text = setting.Label,
            ButtonPressed = setting.Get(),
            FocusMode = Control.FocusModeEnum.None,
        };
    }

    public override string? GetLabel() => _setting.Label;
    public override string? GetTypeKey() => "checkbox";
    public override string? GetStatusString() => _setting.Get() ? "checked" : "unchecked";

    public void Activate()
    {
        var newValue = !_setting.Get();
        _setting.Set(newValue);
        _control.ButtonPressed = newValue;
        SpeechManager.Output(newValue ? "checked" : "unchecked");
    }
}
