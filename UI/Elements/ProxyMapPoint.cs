using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using SayTheSpire2.Localization;
using SayTheSpire2.Map;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.UI.Elements;

[ElementSettingsKey("map_node")]
[AnnouncementOrder(
    typeof(MapMarkedAnnouncement),
    typeof(LabelAnnouncement),
    typeof(FreeTravelAnnouncement),
    typeof(NodeStateAnnouncement),
    typeof(MarkerGuidanceAnnouncement),
    typeof(VotersAnnouncement)
)]
public class ProxyMapPoint : ProxyElement
{
    private MapScreen? _mapScreen;

    public ProxyMapPoint(Control control) : base(control) { }

    /// <summary>
    /// Used by <see cref="ComposeFromView"/> to drive the composer from
    /// non-focus callers (tree map viewer, DescribePoint) so user settings +
    /// reorder apply there too. No Control backing — only announcement
    /// composition uses it.
    /// </summary>
    private ProxyMapPoint() : base() { }

    private NMapPoint? MapPointNode => Control as NMapPoint;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var mp = MapPointNode;
        if (mp == null || mp.Point == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        var view = MapScreen.Current?.BuildPointView(mp.Point);
        if (view == null)
        {
            yield return new LabelAnnouncement(MapNode.GetPointDisplayName(mp.Point));
            yield break;
        }

        foreach (var a in BuildAnnouncements(view))
            yield return a;
    }

    /// <summary>
    /// The shared yield sequence for a map-node <see cref="MapNodeView"/>. Used
    /// by both the focus path (ProxyMapPoint.GetFocusAnnouncements) and the
    /// non-focus callers (TreeMapViewer, MapScreen.DescribePoint) so every
    /// speech source honors the same settings + user order.
    /// </summary>
    public static IEnumerable<Announcement> BuildAnnouncements(MapNodeView view)
    {
        if (view.IsMarked)
            yield return new MapMarkedAnnouncement();

        yield return new LabelAnnouncement(Message.Raw(view.TypeName));

        if (view.IsFreeTravel)
            yield return new FreeTravelAnnouncement();

        if (!string.IsNullOrEmpty(view.State))
            yield return new NodeStateAnnouncement(view.State);

        if (view.OnPathMarkers.Count > 0 || view.DivergingMarkers.Count > 0)
            yield return new MarkerGuidanceAnnouncement(view.OnPathMarkers, view.DivergingMarkers);

        if (view.Voters.Count > 0)
            yield return new VotersAnnouncement(view.Voters);

        // Coordinates are the map node's "position" — yield them as a PositionAnnouncement
        // so the user can reorder or toggle them like any other announcement. Map points
        // have no mod-side Container parent, so UIElement.GetFocusMessage wouldn't inject
        // one for us.
        yield return new PositionAnnouncement(Message.Raw(view.Coordinates));
    }

    /// <summary>
    /// Renders a <see cref="MapNodeView"/> through the announcement pipeline —
    /// honors enabled toggles, per-element overrides, and user-specified order
    /// just like focus-path readings. Uses an ephemeral Control-less proxy as
    /// the composer's context source.
    /// </summary>
    public static string ComposeFromView(MapNodeView view)
    {
        return AnnouncementComposer.Compose(new ProxyMapPoint(), BuildAnnouncements(view)).Resolve();
    }

    public override Message? GetLabel()
    {
        var mp = MapPointNode;
        if (mp == null || mp.Point == null)
            return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        var text = MapScreen.Current?.DescribePoint(mp.Point, includeChoicePrefix: false)
            ?? MapNode.GetPointDisplayName(mp.Point);
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => null;

    public override Message? GetStatusString()
    {
        return null;
    }

    protected override void OnFocus()
    {
        var mp = MapPointNode;
        if (mp?.Point == null) return;

        // If a MapScreen already exists (e.g. from the map key hook), update its start point
        if (MapScreen.Current != null)
        {
            MapScreen.Current.UpdateStartPoint(mp.Point);
            return;
        }

        _mapScreen = new MapScreen(mp.Point);
        ScreenManager.PushScreen(_mapScreen);
    }

    protected override void OnUnfocus()
    {
        // Only remove the screen if we created it
        if (_mapScreen != null)
        {
            ScreenManager.RemoveScreen(_mapScreen);
            _mapScreen = null;
        }
    }
}
