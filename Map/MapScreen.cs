using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Input;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Map;

public class MapScreen : Screen
{
    public static MapScreen? Current { get; private set; }

    private readonly MapHandler _handler = new();
    private readonly PointOfInterestBuffer _pointOfInterestBuffer;
    private TreeMapViewer? _viewer;
    private MapPoint? _startPoint;

    public MapScreen(MapPoint? startPoint)
    {
        _startPoint = startPoint;
        _pointOfInterestBuffer = new PointOfInterestBuffer(_handler, GetCurrentMapPoint, GetAnchorMapPoint);

        // Claim the Ctrl+Arrow actions — on the map these become navigation
        // instead of buffer controls. Not propagated, so DefaultScreen won't see them.
        ClaimAction("buffer_next_item"); // Ctrl+Up -> forward (toward boss)
        ClaimAction("buffer_prev_item"); // Ctrl+Down -> backward (toward start)
        ClaimAction("buffer_next");      // Ctrl+Right -> next branch
        ClaimAction("buffer_prev");      // Ctrl+Left -> prev branch
        ClaimAction("map_poi_prev");
        ClaimAction("map_poi_next");
        ClaimAction("map_poi_toggle_mode");
        ClaimAction("map_toggle_current_marker");
        ClaimAction("map_clear_all_markers");
    }

    public override void OnPush()
    {
        Current = this;

        if (!_handler.Build())
        {
            Log.Error("[AccessibilityMod] MapScreen: Failed to build map graph");
            return;
        }

        _viewer = new TreeMapViewer(_handler);

        if (_startPoint != null)
        {
            var startNode = _handler.GetNode(_startPoint);
            if (startNode != null)
            {
                _viewer.SetStartNode(startNode);
                _pointOfInterestBuffer.SetSelection(startNode.Point);

                if (Settings.ModSettings.GetValue<bool>("map.announce_current_on_open"))
                {
                    SpeechManager.Output(Message.Localized("map_nav", "NAV.CURRENT_LOCATION", new
                    {
                        type = startNode.GetDisplayName(),
                        coordinates = startNode.GetCoordinatesString()
                    }));
                }

                _viewer.MoveForward();
            }
        }

        Log.Info("[AccessibilityMod] MapScreen pushed, viewer ready");
    }

    public override void OnPop()
    {
        _viewer = null;
        if (Current == this) Current = null;
        Log.Info("[AccessibilityMod] MapScreen popped");
    }

    public void UpdateStartPoint(MapPoint point)
    {
        _startPoint = point;
        if (_viewer != null)
        {
            var node = _handler.GetNode(point);
            if (node != null)
            {
                _viewer.SetStartNode(node);
                _pointOfInterestBuffer.SetSelection(node.Point);
            }
        }
    }

    public string? DescribePoint(MapPoint point, bool includeChoicePrefix = true)
    {
        var node = _handler.GetNode(point);
        if (node == null)
            return null;

        return MapNodeAnnouncementFormatter.DescribeNode(node, _handler, includeChoicePrefix: includeChoicePrefix);
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (_viewer == null) return false;

        var handled = false;
        string? result = action.Key switch
        {
            "buffer_next_item" => _viewer.MoveForward(),   // Ctrl+Up
            "buffer_prev_item" => _viewer.MoveBackward(),  // Ctrl+Down
            "buffer_next" => _viewer.NextBranch(),          // Ctrl+Right
            "buffer_prev" => _viewer.PreviousBranch(),      // Ctrl+Left
            "map_poi_prev" => TryMovePoi(previous: true, _viewer),
            "map_poi_next" => TryMovePoi(previous: false, _viewer),
            "map_poi_toggle_mode" => TryTogglePoiMode(_viewer),
            "map_toggle_current_marker" => TryToggleCurrentMarker(_viewer),
            "map_clear_all_markers" => TryClearAllMarkers(),
            _ => null,
        };

        if (action.Key is "map_poi_prev" or "map_poi_next" or "map_poi_toggle_mode"
            or "map_toggle_current_marker" or "map_clear_all_markers")
            handled = true;

        if (result != null)
        {
            SyncPoiSelectionToViewer();
            SpeechManager.Output(Message.Raw(result));
            return true;
        }

        return handled;
    }

    private static MapPoint? GetCurrentMapPoint()
    {
        try
        {
            return RunManager.Instance.DebugOnlyGetState()?.CurrentMapPoint;
        }
        catch
        {
            return null;
        }
    }

    private MapPoint? GetAnchorMapPoint()
    {
        return _viewer?.CurrentNode?.Point ?? _startPoint ?? GetCurrentMapPoint();
    }

    private void SyncPoiSelectionToViewer()
    {
        _pointOfInterestBuffer.SetSelection(_viewer?.CurrentNode?.Point);
    }

    private string? TryMovePoi(bool previous, TreeMapViewer viewer)
    {
        var moved = previous
            ? _pointOfInterestBuffer.MovePrevious()
            : _pointOfInterestBuffer.MoveNext();
        if (!moved)
            return null;

        var node = _pointOfInterestBuffer.SelectedNode;
        return node == null ? null : viewer.JumpToNode(node);
    }

    private string TryTogglePoiMode(TreeMapViewer viewer)
    {
        var modeLabel = _pointOfInterestBuffer.ToggleMode();
        var node = _pointOfInterestBuffer.SelectedNode;
        if (node == null)
            return modeLabel;

        var announcement = viewer.JumpToNode(node);
        return announcement == null ? modeLabel : $"{modeLabel}, {announcement}";
    }

    private static string? TryToggleCurrentMarker(TreeMapViewer viewer)
    {
        var node = viewer.CurrentNode;
        if (node == null)
            return null;

        if (MapMarkerState.IsMarked(node.Point))
            MapMarkerState.Clear(node.Point);
        else
            MapMarkerState.Mark(node.Point);

        return viewer.JumpToNode(node);
    }

    private static string TryClearAllMarkers()
    {
        MapMarkerState.ClearAll();
        return Ui("MAP_MARKERS.CLEARED_ALL");
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
