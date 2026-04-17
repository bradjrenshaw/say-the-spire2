using System;
using System.Collections.Generic;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(ControlValueAnnouncement)
)]
public sealed class StatsValueElement : UIElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        if (!_suppressLabelForCurrentAnnouncement)
        {
            var label = _label();
            if (label != null)
                yield return new LabelAnnouncement(label);
        }

        var values = GetNormalizedValues();
        if (values.Count > 0)
        {
            if (_valueIndex >= values.Count)
                _valueIndex = values.Count - 1;
            yield return new ControlValueAnnouncement(values[_valueIndex]);
        }
    }

    private readonly Func<Message?> _label;
    private readonly Func<IReadOnlyList<string>> _values;
    private int _valueIndex;
    private bool _suppressLabelForCurrentAnnouncement;

    public StatsValueElement(Func<Message?> label, Func<IReadOnlyList<string>> values)
    {
        _label = label;
        _values = values;
    }

    public override Message? GetLabel()
    {
        if (_suppressLabelForCurrentAnnouncement)
            return null;

        return _label();
    }

    public override Message? GetStatusString()
    {
        var values = GetNormalizedValues();
        if (values.Count == 0)
            return null;

        if (_valueIndex >= values.Count)
            _valueIndex = values.Count - 1;

        return Message.Raw(values[_valueIndex]);
    }

    public bool MoveValue(int delta)
    {
        var values = GetNormalizedValues();
        if (values.Count <= 1)
            return false;

        var next = Math.Clamp(_valueIndex + delta, 0, values.Count - 1);
        if (next == _valueIndex)
            return false;

        _valueIndex = next;
        _suppressLabelForCurrentAnnouncement = true;
        return true;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            var label = GetLabel()?.Resolve();
            if (!string.IsNullOrEmpty(label))
                uiBuffer.Add(label);
            var status = GetStatusString()?.Resolve();
            if (!string.IsNullOrEmpty(status))
                uiBuffer.Add(status);
            buffers.EnableBuffer("ui", true);
        }

        _suppressLabelForCurrentAnnouncement = false;
        return "ui";
    }

    private IReadOnlyList<string> GetNormalizedValues()
    {
        var values = _values();
        if (values.Count == 0)
            _valueIndex = 0;
        else if (_valueIndex >= values.Count)
            _valueIndex = values.Count - 1;

        return values;
    }

    protected override void OnFocus()
    {
        _suppressLabelForCurrentAnnouncement = false;
    }

    protected override void OnUnfocus()
    {
        _suppressLabelForCurrentAnnouncement = false;
    }
}
