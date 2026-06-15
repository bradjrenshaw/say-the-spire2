using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Help;
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

    // View-only maps (opened from the top bar / M key in combat & other rooms)
    // have no selectable nodes, so the game leaves focus on the underlying
    // room control. There the buffer keys should always browse the map.
    // Interactive maps (room transitions) put focus on a selectable node, and
    // moving focus to a non-node control like the top bar should restore
    // normal buffer behavior.
    private readonly bool _isViewOnly;

    public MapScreen(MapPoint? startPoint, bool isViewOnly = false)
    {
        _isViewOnly = isViewOnly;
        _startPoint = startPoint;
        _pointOfInterestBuffer = new PointOfInterestBuffer(_handler, GetCurrentMapPoint, GetAnchorMapPoint);

        // Claim the Ctrl+Arrow actions — on the map these become navigation
        // instead of buffer controls. Gated on ShouldHandleMapBuffers so they
        // only drive the map when a map node is focused or nothing is focused
        // (view-only map from combat); when focus is on a non-map control like
        // the top bar, the claim falls through to DefaultScreen and the buffers
        // behave normally. Not propagated, so DefaultScreen won't see them while
        // the map is actually handling them.
        ClaimAction("buffer_next_item", focusedOnly: true, condition: ShouldHandleMapBuffers); // Ctrl+Up -> forward (toward boss)
        ClaimAction("buffer_prev_item", focusedOnly: true, condition: ShouldHandleMapBuffers); // Ctrl+Down -> backward (toward start)
        ClaimAction("buffer_next", focusedOnly: true, condition: ShouldHandleMapBuffers);      // Ctrl+Right -> next branch
        ClaimAction("buffer_prev", focusedOnly: true, condition: ShouldHandleMapBuffers);      // Ctrl+Left -> prev branch
        ClaimAction("map_poi_prev");
        ClaimAction("map_poi_next");
        ClaimAction("map_poi_toggle_mode");
        ClaimAction("map_toggle_current_marker");
        ClaimAction("map_clear_all_markers");
    }

    public override void OnPush()
    {
        if (!_handler.Build())
        {
            Log.Error("[AccessibilityMod] MapScreen: Failed to build map graph");
            return;
        }

        // Set Current after Build succeeds — a failed build leaves _handler /
        // _viewer in a half-state, and other code reading MapScreen.Current
        // would crash chasing those.
        Current = this;

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
                        coordinates = startNode.GetCoordinates()
                    }));
                }

                _viewer.MoveForward();
            }
        }

        Log.Info("[AccessibilityMod] MapScreen pushed, viewer ready");
    }

    public override void OnUpdate()
    {
        WireRelicRowToMap();
    }

    public override void OnPop()
    {
        RestoreRelicRow();
        _viewer = null;
        if (Current == this) Current = null;
        Log.Info("[AccessibilityMod] MapScreen popped");
    }

    // The beta routes the relic row's down-focus through a shared
    // %ActiveScreenProxy node whose FocusEntered is supposed to forward focus
    // into the open map — but that hop doesn't land for us (down from the relic
    // row just does nothing; the game's controller focus handling is famously
    // unreliable here). Bypass it: while an interactive map is open in
    // singleplayer, point each relic holder's bottom focus-neighbor straight at
    // the map's default node, and restore the proxy target when the map closes.
    //
    // Singleplayer only: in multiplayer the relic row wires its bottom neighbor
    // through the player-state column, which we must not disturb.
    private ulong _wiredRelicTargetId;

    private void WireRelicRowToMap()
    {
        if (_isViewOnly) return;
        if (!Multiplayer.MultiplayerHelper.IsSingleplayerOrFakeMultiplayer()) return;

        if (NMapScreen.Instance?.DefaultFocusedControl is not NMapPoint node)
            return;
        if (node.GetInstanceId() == _wiredRelicTargetId) return;

        var relics = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes;
        if (relics == null || relics.Count == 0) return;

        var path = node.GetPath();
        foreach (var relic in relics)
            relic.FocusNeighborBottom = path;
        _wiredRelicTargetId = node.GetInstanceId();
    }

    private void RestoreRelicRow()
    {
        if (_wiredRelicTargetId == 0) return;
        _wiredRelicTargetId = 0;

        var proxy = NRun.Instance?.GlobalUi?.TopBar?.ActiveScreenProxy;
        var relics = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes;
        if (proxy == null || relics == null) return;

        var path = proxy.GetPath();
        foreach (var relic in relics)
            relic.FocusNeighborBottom = path;
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

    public override List<HelpMessage> GetHelpMessages() => new()
    {
        new TextHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_NAV", "Map navigation uses buffer controls (Ctrl+directional keys by default). Buffer controls are remapped on this screen."), exclusive: true),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_FORWARD", "Move Forward (toward boss)"), "buffer_next_item"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_BACKWARD", "Move Backward (toward start)"), "buffer_prev_item"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_NEXT_BRANCH", "Next Branch"), "buffer_next"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_PREV_BRANCH", "Previous Branch"), "buffer_prev"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_NEXT_POI", "Next Point of Interest"), "map_poi_next"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_PREV_POI", "Previous Point of Interest"), "map_poi_prev"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_TOGGLE_POI_MODE", "Toggle POI Mode (Reachable / All)"), "map_poi_toggle_mode"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_TOGGLE_MARKER", "Toggle Marker on Current Node"), "map_toggle_current_marker"),
        new ControlHelpMessage(LocalizationManager.GetOrDefault("ui", "HELP.MAP_CLEAR_MARKERS", "Clear All Markers"), "map_clear_all_markers"),
    };

    public Message? DescribePoint(MapPoint point, bool includeChoicePrefix = true)
    {
        var node = _handler.GetNode(point);
        if (node == null)
            return null;

        return MapNodeAnnouncementFormatter.DescribeNode(node, _handler, includeChoicePrefix: includeChoicePrefix,
            travelOrigin: _handler.CurrentNode);
    }

    /// <summary>
    /// Structured view of a point for the announcement pipeline (ProxyMapPoint's
    /// focus-string composition). Non-focus callers should use DescribePoint.
    /// </summary>
    public MapNodeView? BuildPointView(MapPoint point)
    {
        var node = _handler.GetNode(point);
        if (node == null)
            return null;

        return MapNodeAnnouncementFormatter.BuildView(node, _handler, travelOrigin: _handler.CurrentNode);
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        if (_viewer == null) return false;

        var handled = false;
        Message? result = action.Key switch
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
            SpeechManager.Output(result);
            return true;
        }

        return handled;
    }

    /// <summary>
    /// Whether the map screen should consume the buffer (Ctrl+arrow) keys for
    /// map navigation. A view-only map (opened from the top bar / M key) has no
    /// selectable nodes and the game keeps focus on the underlying room
    /// control, so always browse the map there. On an interactive map, drive
    /// the map only while a map node is focused; if focus is on a non-map
    /// control like the top bar, fall through so the buffers work normally.
    /// </summary>
    private bool ShouldHandleMapBuffers()
    {
        if (_isViewOnly) return true;

        var focused = NRun.Instance?.GetViewport()?.GuiGetFocusOwner();
        for (Node? node = focused; node != null; node = node.GetParent())
        {
            if (node is NMapPoint) return true;
        }
        return false;
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

    private Message? TryMovePoi(bool previous, TreeMapViewer viewer)
    {
        var moved = previous
            ? _pointOfInterestBuffer.MovePrevious()
            : _pointOfInterestBuffer.MoveNext();
        if (!moved)
            return null;

        var node = _pointOfInterestBuffer.SelectedNode;
        return node == null ? null : viewer.JumpToNode(node);
    }

    private Message TryTogglePoiMode(TreeMapViewer viewer)
    {
        var modeLabel = Message.Raw(_pointOfInterestBuffer.ToggleMode());
        var node = _pointOfInterestBuffer.SelectedNode;
        if (node == null)
            return modeLabel;

        var announcement = viewer.JumpToNode(node);
        return announcement == null ? modeLabel : Message.Join(", ", modeLabel, announcement);
    }

    private static Message? TryToggleCurrentMarker(TreeMapViewer viewer)
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

    private static Message TryClearAllMarkers()
    {
        MapMarkerState.ClearAll();
        return Message.Localized("ui", "MAP_MARKERS.CLEARED_ALL");
    }
}
