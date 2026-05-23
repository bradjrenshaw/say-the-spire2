using System;
using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public class ButtonElement : UIElement
{
    // Share settings / [AnnouncementOrder] with ProxyButton — both are buttons
    // to the end user, no reason to configure them separately.
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    private readonly Button _control;
    private readonly string _label;

    public Action? OnActivated { get; set; }
    public Node Node => _control;

    /// <summary>
    /// When true the button stays focusable and announces itself (so the user
    /// knows it exists) but activating it does nothing, and a "disabled"
    /// status is appended. Used for entries with nothing to configure — e.g.
    /// hotkey announcements that have no options.
    /// </summary>
    public bool Disabled { get; set; }

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
        if (Disabled)
            yield return new StatusAnnouncement(Message.Localized("ui", "LABELS.DISABLED"));
    }

    public override Message? GetLabel() => Message.Raw(_label);
    public override string? GetTypeKey() => "button";

    public void Activate()
    {
        if (Disabled) return;
        OnActivated?.Invoke();
    }
}
