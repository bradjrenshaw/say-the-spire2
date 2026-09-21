using Godot;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Last-resort proxy for a control no specific proxy type recognizes.
/// Behaves exactly like a generic button (reads the node name when it's
/// meaningful) so unknown screens remain navigable, but its type marks the
/// element as anonymous: the focus watchdog refuses to announce these,
/// because the game routinely parks Godot focus on internal layout controls
/// ("Hitbox", "Loot", "CardHolderContainer") that were never user-facing
/// elements — reading their node names is noise, not information.
/// </summary>
public class ProxyFallbackControl : ProxyButton
{
    // Share announcement settings/order with ProxyButton — to the user this
    // is just a button-shaped unknown, not a separately configurable thing.
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    public ProxyFallbackControl(Control control) : base(control) { }
}
