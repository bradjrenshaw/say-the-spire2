using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class CheckboxElement : UIElement
{
    // Share settings / [AnnouncementOrder] with ProxyCheckbox.
    public override System.Type AnnouncementOrderType => typeof(ProxyCheckbox);

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield return new LabelAnnouncement(_setting.Label);
        yield return new TypeAnnouncement("checkbox");
        yield return new ControlValueAnnouncement(
            _setting.Get()
                ? Message.Localized("ui", "CHECKBOX.CHECKED")
                : Message.Localized("ui", "CHECKBOX.UNCHECKED"));
    }

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
    public override Message? GetStatusString() => _setting.Get() ? Message.Localized("ui", "CHECKBOX.CHECKED") : Message.Localized("ui", "CHECKBOX.UNCHECKED");

    public void Activate()
    {
        var newValue = !_setting.Get();
        _setting.Set(newValue);
        _control.SetPressedNoSignal(newValue);
        SpeechManager.Output(newValue ? Message.Localized("ui", "CHECKBOX.CHECKED") : Message.Localized("ui", "CHECKBOX.UNCHECKED"));
    }

    /// <summary>
    /// Called from mouse click. The CheckBox already toggled itself,
    /// so just sync the setting to match.
    /// </summary>
    public void SyncFromControl()
    {
        _setting.Set(_control.ButtonPressed);
        SpeechManager.Output(_control.ButtonPressed ? Message.Localized("ui", "CHECKBOX.CHECKED") : Message.Localized("ui", "CHECKBOX.UNCHECKED"));
    }
}
