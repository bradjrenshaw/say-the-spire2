using Godot;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Elements;

public class SliderElement : UIElement
{
    private readonly HSlider _control;
    private readonly IntSetting _setting;
    private bool _suppressSync;

    public Node Node => _control;

    public SliderElement(IntSetting setting)
    {
        _setting = setting;
        _control = new HSlider
        {
            MinValue = setting.Min,
            MaxValue = setting.Max,
            Value = setting.Get(),
            Step = 1,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(200, 0),
        };
    }

    public override string? GetLabel() => _setting.Label;
    public override string? GetTypeKey() => "slider";
    public override string? GetStatusString() => _setting.Get().ToString();

    public void Increment()
    {
        var newValue = System.Math.Min(_setting.Get() + _setting.Step, _setting.Max);
        if (newValue == _setting.Get()) return;
        _setting.Set(newValue);
        _suppressSync = true;
        _control.Value = newValue;
        _suppressSync = false;
        SpeechManager.Output(newValue.ToString());
    }

    public void Decrement()
    {
        var newValue = System.Math.Max(_setting.Get() - _setting.Step, _setting.Min);
        if (newValue == _setting.Get()) return;
        _setting.Set(newValue);
        _suppressSync = true;
        _control.Value = newValue;
        _suppressSync = false;
        SpeechManager.Output(newValue.ToString());
    }

    public void SyncFromControl()
    {
        if (_suppressSync) return;
        var value = (int)_control.Value;
        _setting.Set(value);
        SpeechManager.Output(value.ToString());
    }
}
