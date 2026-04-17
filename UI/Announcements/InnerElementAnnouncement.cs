using SayTheSpire2.Localization;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Delegates to a nested UIElement's full focus message. Used by composite
/// proxies (merchant slot, reward button) that wrap another element — the
/// outer proxy can treat the inner as a single opaque announcement without
/// enumerating the inner's individual announcements.
/// </summary>
public sealed class InnerElementAnnouncement : Announcement
{
    private readonly UIElement _inner;

    public InnerElementAnnouncement(UIElement inner) { _inner = inner; }

    public override string Key => "inner";
    public override string Suffix => ",";
    public override Message Render() => _inner.GetFocusMessage();
}
