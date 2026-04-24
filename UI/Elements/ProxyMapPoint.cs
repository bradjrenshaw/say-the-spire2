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

        if (view.IsMarked)
            yield return new MapMarkedAnnouncement();

        yield return new LabelAnnouncement(Message.Localized("map_nav", "NAV.NODE", new
        {
            type = view.TypeName,
            coordinates = view.Coordinates
        }));

        if (view.IsFreeTravel)
            yield return new FreeTravelAnnouncement();

        if (!string.IsNullOrEmpty(view.State))
            yield return new NodeStateAnnouncement(view.State);

        if (view.OnPathMarkers.Count > 0 || view.DivergingMarkers.Count > 0)
            yield return new MarkerGuidanceAnnouncement(view.OnPathMarkers, view.DivergingMarkers);

        if (view.Voters.Count > 0)
            yield return new VotersAnnouncement(view.Voters);
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
