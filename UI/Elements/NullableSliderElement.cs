using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Renders a NullableIntSetting as a regular-looking slider: shows the resolved
/// value (explicit override or inherited global), and any user change writes
/// an explicit value. Inherit state is invisible in the UI; users get back to
/// it via the category-level reset button.
/// </summary>
public class NullableSliderElement : UIElement
{
    // Share settings / [AnnouncementOrder] with ProxySlider.
    public override System.Type AnnouncementOrderType => typeof(ProxySlider);

    private readonly HSlider _control;
    private readonly NullableIntSetting _setting;
    private readonly System.Action<int> _onResolvedChanged;
    private bool _suppressSync;

    public Node Node => _control;

    public NullableSliderElement(NullableIntSetting setting)
    {
        _setting = setting;
        _control = new HSlider
        {
            MinValue = setting.Fallback.Min,
            MaxValue = setting.Fallback.Max,
            Value = setting.Resolved,
            Step = 1,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(200, 0),
        };

        // Keep UI in sync when resolved changes via fallback or reset.
        _onResolvedChanged = v =>
        {
            if (!GodotObject.IsInstanceValid(_control)) return;
            _suppressSync = true;
            _control.Value = v;
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
        yield return new TypeAnnouncement("slider");
        yield return new StatusAnnouncement(_setting.Resolved.ToString());
    }

    public override Message? GetLabel() => Message.Raw(_setting.Label);
    public override string? GetTypeKey() => "slider";
    public override Message? GetStatusString() => Message.Raw(_setting.Resolved.ToString());

    public void Increment()
    {
        var next = System.Math.Min(_setting.Resolved + _setting.Fallback.Step, _setting.Fallback.Max);
        if (next == _setting.Resolved) return;
        _setting.SetExplicit(next);
        _suppressSync = true;
        _control.Value = next;
        _suppressSync = false;
        SpeechManager.Output(Message.Raw(next.ToString()));
    }

    public void Decrement()
    {
        var next = System.Math.Max(_setting.Resolved - _setting.Fallback.Step, _setting.Fallback.Min);
        if (next == _setting.Resolved) return;
        _setting.SetExplicit(next);
        _suppressSync = true;
        _control.Value = next;
        _suppressSync = false;
        SpeechManager.Output(Message.Raw(next.ToString()));
    }

    public void SyncFromControl()
    {
        if (_suppressSync) return;
        var value = (int)_control.Value;
        _setting.SetExplicit(value);
        SpeechManager.Output(Message.Raw(value.ToString()));
    }
}
