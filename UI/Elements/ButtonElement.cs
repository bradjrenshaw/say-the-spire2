using System;
using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement)
)]
public class ButtonElement : UIElement
{
    private readonly Button _control;
    private readonly string _label;

    public Action? OnActivated { get; set; }
    public Node Node => _control;

    public ButtonElement(string label)
    {
        _label = label;
        _control = new Button
        {
            Text = label,
            FocusMode = Control.FocusModeEnum.None,
        };
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield return new LabelAnnouncement(_label);
        yield return new TypeAnnouncement("button");
    }

    public override Message? GetLabel() => Message.Raw(_label);
    public override string? GetTypeKey() => "button";

    public void Activate()
    {
        OnActivated?.Invoke();
    }
}
