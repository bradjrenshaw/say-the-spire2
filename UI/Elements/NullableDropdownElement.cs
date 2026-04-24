using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Renders a NullableChoiceSetting as a regular-looking dropdown button: shows
/// the resolved selection (explicit override or inherited global), and any
/// user change writes an explicit value. Inherit state is invisible; users
/// get back to it via the category-level reset button.
/// </summary>
public class NullableDropdownElement : UIElement
{
    // Share settings / [AnnouncementOrder] with ProxyDropdown.
    public override System.Type AnnouncementOrderType => typeof(ProxyDropdown);

    private readonly Button _control;
    private readonly NullableChoiceSetting _setting;
    private readonly System.Action<string> _onResolvedChanged;

    public Node Node => _control;
    public NullableChoiceSetting Setting => _setting;

    public NullableDropdownElement(NullableChoiceSetting setting)
    {
        _setting = setting;
        _control = new Button
        {
            Text = GetButtonText(),
            FocusMode = Control.FocusModeEnum.None,
        };

        _onResolvedChanged = _ =>
        {
            if (GodotObject.IsInstanceValid(_control))
                _control.Text = GetButtonText();
        };
        setting.ResolvedChanged += _onResolvedChanged;
    }

    public override void Detach()
    {
        _setting.ResolvedChanged -= _onResolvedChanged;
        base.Detach();
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield return new LabelAnnouncement(_setting.Label);
        yield return new TypeAnnouncement("dropdown");
        var selected = _setting.GetSelectedChoice();
        yield return new StatusAnnouncement(selected?.Label ?? _setting.Resolved);
    }

    public override Message? GetLabel() => Message.Raw(_setting.Label);
    public override string? GetTypeKey() => "dropdown";
    public override Message? GetStatusString()
    {
        var selected = _setting.GetSelectedChoice();
        return Message.Raw(selected?.Label ?? _setting.Resolved);
    }

    private string GetButtonText()
    {
        var selected = _setting.GetSelectedChoice();
        return $"{_setting.Label}: {selected?.Label ?? _setting.Resolved}";
    }
}
