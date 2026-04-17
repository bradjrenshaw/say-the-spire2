using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement)
)]
public class DropdownElement : UIElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield return new LabelAnnouncement(_setting.Label);
        yield return new TypeAnnouncement("dropdown");
        var selected = _setting.GetSelected();
        yield return new ControlValueAnnouncement(selected?.Label ?? _setting.Get());
    }

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

    public override Message? GetLabel() => Message.Raw(_setting.Label);
    public override string? GetTypeKey() => "dropdown";

    public override Message? GetStatusString()
    {
        var selected = _setting.GetSelected();
        return Message.Raw(selected?.Label ?? _setting.Get());
    }

    private string GetButtonText()
    {
        var selected = _setting.GetSelected();
        return $"{_setting.Label}: {selected?.Label ?? _setting.Get()}";
    }
}
