using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// The Dread lobby's "Begin Rotating Streak" tickbox. A checkbox like any
/// other, plus the hover tip the game shows on it (what a rotating streak
/// is), since nothing else on the screen explains it.
/// </summary>
public class ProxyDreadRotationTickbox : ProxyCheckbox
{
    private readonly DreadLobbyView _lobby;

    public ProxyDreadRotationTickbox(Control control, DreadLobbyView lobby) : base(control)
    {
        _lobby = lobby;
    }

    // Share settings / [AnnouncementOrder] with ProxyCheckbox.
    public override System.Type AnnouncementOrderType => typeof(ProxyCheckbox);

    // The game hides the tickbox on the Rotating button and while a rotating
    // streak is running; it then doesn't count towards list positions.
    public override bool IsVisible => Control != null && Control.IsVisibleInTree();

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        foreach (var announcement in base.GetFocusAnnouncements())
            yield return announcement;

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel() => Message.Raw(_lobby.TickboxLabel);

    public override Message? GetTooltip()
    {
        var (title, description) = DreadLobbyView.TickboxTip;
        return Message.Raw($"{title}: {description}");
    }
}
