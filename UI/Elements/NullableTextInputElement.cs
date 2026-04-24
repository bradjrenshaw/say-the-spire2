using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Renders a NullableStringSetting as a regular-looking line-edit text field:
/// shows the resolved value (explicit override or inherited global), and any
/// edit — including clearing the field to empty — writes an explicit value.
/// Inherit state is invisible; users get back to it via the category-level
/// reset button. This preserves the plan-doc distinction between null
/// (inherit) and "" (explicit empty).
/// </summary>
public class NullableTextInputElement : UIElement
{
    // Share settings / [AnnouncementOrder] with ProxyTextInput.
    public override System.Type AnnouncementOrderType => typeof(ProxyTextInput);

    private readonly LineEdit _control;
    private readonly NullableStringSetting _setting;
    private readonly System.Action<string> _onResolvedChanged;
    private bool _suppressSync;

    public Node Node => _control;

    public NullableTextInputElement(NullableStringSetting setting)
    {
        _setting = setting;
        _control = new LineEdit
        {
            Text = setting.Resolved,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(200, 0),
        };

        _onResolvedChanged = v =>
        {
            if (!GodotObject.IsInstanceValid(_control)) return;
            _suppressSync = true;
            _control.Text = v;
            _suppressSync = false;
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
        yield return new TypeAnnouncement("textbox");
        var resolved = _setting.Resolved;
        if (!string.IsNullOrEmpty(resolved))
            yield return new StatusAnnouncement(resolved);
    }

    public override Message? GetLabel() => Message.Raw(_setting.Label);
    public override string? GetTypeKey() => "textbox";
    public override Message? GetStatusString()
    {
        var resolved = _setting.Resolved;
        return string.IsNullOrEmpty(resolved) ? null : Message.Raw(resolved);
    }

    public void SyncFromControl()
    {
        if (_suppressSync) return;
        _setting.SetExplicit(_control.Text);
        SpeechManager.Output(Message.Raw(_control.Text));
    }
}
