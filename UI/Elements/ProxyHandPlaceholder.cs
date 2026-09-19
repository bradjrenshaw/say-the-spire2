using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// NPlayerHand's card holder container ("%CardHolderContainer", a plain
/// Control). The game uses it as a focus parking spot for an empty hand:
/// NPlayerHand.DefaultFocusedControl falls back to it only when no card
/// holders are active (e.g. after the hand is discarded at end of turn), and
/// the container's own FocusEntered handler redirects focus to a card
/// whenever one exists. Focus resting here therefore means "empty hand
/// placeholder" — announce nothing rather than reading its node name.
/// </summary>
public class ProxyHandPlaceholder : ProxyElement
{
    public ProxyHandPlaceholder(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield break;
    }

    public override Message? GetLabel() => null;
}
