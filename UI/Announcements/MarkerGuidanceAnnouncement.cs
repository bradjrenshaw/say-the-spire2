using System.Collections.Generic;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Map-marker guidance: lists markers reachable along this node's path
/// (on-path) and markers given up by choosing this node over its siblings
/// (diverges-from). Always one unit — users either want marker guidance
/// or they don't; splitting the two halves would be unnecessary granularity.
/// Empty when neither list has entries.
/// </summary>
public sealed class MarkerGuidanceAnnouncement : Announcement
{
    private readonly IReadOnlyList<string> _onPath;
    private readonly IReadOnlyList<string> _diverges;

    public MarkerGuidanceAnnouncement(IReadOnlyList<string> onPath, IReadOnlyList<string> diverges)
    {
        _onPath = onPath;
        _diverges = diverges;
    }

    public override string Key => "marker_guidance";
    public override string Suffix => ",";

    public override Message Render()
    {
        var parts = new List<Message>();
        if (_onPath.Count > 0)
            parts.Add(Message.Localized("map_nav", "NAV.ON_PATH_TO", new
            {
                markers = string.Join(", ", _onPath)
            }));
        if (_diverges.Count > 0)
            parts.Add(Message.Localized("map_nav", "NAV.DIVERGES_FROM", new
            {
                markers = string.Join(", ", _diverges)
            }));
        return parts.Count > 0 ? Message.Join(", ", parts.ToArray()) : Message.Empty;
    }
}
