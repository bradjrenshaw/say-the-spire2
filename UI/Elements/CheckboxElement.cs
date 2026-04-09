using Godot;
using SayTheSpire2.Localization;
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

    public override Message? GetLabel() => Message.Raw(_setting.Label);
    public override string? GetTypeKey() => "checkbox";
    public override Message? GetStatusString() => Message.Raw(_setting.Get() ? LocalizationManager.GetOrDefault("ui", "CHECKBOX.CHECKED", "checked") : LocalizationManager.GetOrDefault("ui", "CHECKBOX.UNCHECKED", "unchecked"));

    public void Activate()
    {
        var newValue = !_setting.Get();
        _setting.Set(newValue);
        _control.SetPressedNoSignal(newValue);
        SpeechManager.Output(Message.Raw(newValue ? LocalizationManager.GetOrDefault("ui", "CHECKBOX.CHECKED", "checked") : LocalizationManager.GetOrDefault("ui", "CHECKBOX.UNCHECKED", "unchecked")));
    }

    /// <summary>
    /// Called from mouse click. The CheckBox already toggled itself,
    /// so just sync the setting to match.
    /// </summary>
    public void SyncFromControl()
    {
        _setting.Set(_control.ButtonPressed);
        SpeechManager.Output(Message.Raw(_control.ButtonPressed ? LocalizationManager.GetOrDefault("ui", "CHECKBOX.CHECKED", "checked") : LocalizationManager.GetOrDefault("ui", "CHECKBOX.UNCHECKED", "unchecked")));
    }
}
