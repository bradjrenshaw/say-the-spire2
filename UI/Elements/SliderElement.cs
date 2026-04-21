using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class SliderElement : UIElement
{
    // Share settings / [AnnouncementOrder] with ProxySlider.
    public override System.Type AnnouncementOrderType => typeof(ProxySlider);

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield return new LabelAnnouncement(_setting.Label);
        yield return new TypeAnnouncement("slider");
        yield return new ControlValueAnnouncement(_setting.Get().ToString());
    }

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

    public override Message? GetLabel() => Message.Raw(_setting.Label);
    public override string? GetTypeKey() => "slider";
    public override Message? GetStatusString() => Message.Raw(_setting.Get().ToString());

    public void Increment()
    {
        var newValue = System.Math.Min(_setting.Get() + _setting.Step, _setting.Max);
        if (newValue == _setting.Get()) return;
        _setting.Set(newValue);
        _suppressSync = true;
        _control.Value = newValue;
        _suppressSync = false;
        SpeechManager.Output(Message.Raw(newValue.ToString()));
    }

    public void Decrement()
    {
        var newValue = System.Math.Max(_setting.Get() - _setting.Step, _setting.Min);
        if (newValue == _setting.Get()) return;
        _setting.Set(newValue);
        _suppressSync = true;
        _control.Value = newValue;
        _suppressSync = false;
        SpeechManager.Output(Message.Raw(newValue.ToString()));
    }

    public void SyncFromControl()
    {
        if (_suppressSync) return;
        var value = (int)_control.Value;
        _setting.Set(value);
        SpeechManager.Output(Message.Raw(value.ToString()));
    }
}
