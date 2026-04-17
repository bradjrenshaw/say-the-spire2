using System;
using System.Collections.Generic;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ActionElement : UIElement
{
    private readonly Func<string?> _label;
    private readonly Func<string?>? _typeKey;
    private readonly Func<string?>? _status;
    private readonly Func<string?>? _tooltip;
    private readonly Func<bool>? _isVisible;
    private readonly Action? _onActivated;

    public ActionElement(
        Func<string?> label,
        Func<string?>? status = null,
        Func<string?>? tooltip = null,
        Func<string?>? typeKey = null,
        Func<bool>? isVisible = null,
        Action? onActivated = null)
    {
        _label = label;
        _status = status;
        _tooltip = tooltip;
        _typeKey = typeKey;
        _isVisible = isVisible;
        _onActivated = onActivated;
    }

    public override bool IsVisible => _isVisible?.Invoke() ?? true;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = _label();
        if (!string.IsNullOrEmpty(label))
            yield return new LabelAnnouncement(label);

        var typeKey = _typeKey?.Invoke();
        if (!string.IsNullOrEmpty(typeKey))
            yield return new TypeAnnouncement(typeKey);

        var status = _status?.Invoke();
        if (!string.IsNullOrEmpty(status))
            yield return new ControlValueAnnouncement(status);

        var tooltip = _tooltip?.Invoke();
        if (!string.IsNullOrEmpty(tooltip))
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel() { var v = _label(); return v != null ? Message.Raw(v) : null; }
    public override string? GetTypeKey() => _typeKey?.Invoke();
    public override Message? GetStatusString() { var v = _status?.Invoke(); return v != null ? Message.Raw(v) : null; }
    public override Message? GetTooltip() { var v = _tooltip?.Invoke(); return v != null ? Message.Raw(v) : null; }

    public bool Activate()
    {
        if (_onActivated == null)
            return false;

        _onActivated();
        return true;
    }
}
