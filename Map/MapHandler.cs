using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace SayTheSpire2.Map;

public class MapHandler
{
    private static readonly FieldInfo? MapField =
        typeof(NMapScreen).GetField("_map", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? RunStateField =
        typeof(NMapScreen).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? MapPointDictField =
        typeof(NMapScreen).GetField("_mapPointDictionary", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Dictionary<MapPoint, MapNode> _nodeMap = new();
    private readonly List<MapEdge> _edges = new();

    public IReadOnlyDictionary<MapPoint, MapNode> Nodes => _nodeMap;
    public IReadOnlyList<MapEdge> Edges => _edges;

    public bool Build()
    {
        _nodeMap.Clear();
        _edges.Clear();

        var screen = NMapScreen.Instance;
        if (screen == null)
        {
            Log.Error("[AccessibilityMod] MapHandler: NMapScreen.Instance is null");
            return false;
        }

        var map = MapField?.GetValue(screen) as ActMap;
        var runState = RunStateField?.GetValue(screen) as RunState;
        var pointDict = MapPointDictField?.GetValue(screen) as Dictionary<MapCoord, NMapPoint>;

        if (map == null || pointDict == null)
        {
            Log.Error("[AccessibilityMod] MapHandler: Could not read map data from NMapScreen");
            return false;
        }

        // Build nodes
        foreach (var mapPoint in map.GetAllMapPoints())
        {
            var state = GetStateFromUI(mapPoint.coord, pointDict);
            var node = CreateNode(mapPoint, state, map, runState);
            _nodeMap[mapPoint] = node;
        }

        // Include boss and starting points if not already covered
        EnsureNode(map.BossMapPoint, map, runState, pointDict);
        if (map.SecondBossMapPoint != null)
            EnsureNode(map.SecondBossMapPoint, map, runState, pointDict);
        EnsureNode(map.StartingMapPoint, map, runState, pointDict);

        // Build edges from Children relationships
        foreach (var (mapPoint, node) in _nodeMap)
        {
            foreach (var child in mapPoint.Children)
            {
                if (_nodeMap.TryGetValue(child, out var childNode))
                {
                    var edge = new MapEdge(node, childNode);
                    _edges.Add(edge);
                    node.ForwardEdges.Add(edge);
                    childNode.BackwardEdges.Add(edge);
                }
            }
        }

        Log.Info($"[AccessibilityMod] MapHandler: Built graph with {_nodeMap.Count} nodes, {_edges.Count} edges");
        return true;
    }

    public MapNode? GetNode(MapPoint point)
    {
        return _nodeMap.TryGetValue(point, out var node) ? node : null;
    }

    public MapNode? GetNode(MapCoord coord)
    {
        foreach (var node in _nodeMap.Values)
        {
            if (node.Point.coord.Equals(coord))
                return node;
        }

        return null;
    }

    /// <summary>
    /// Get all nodes at a given row, sorted by column.
    /// </summary>
    public List<MapNode> GetNodesAtRow(int row)
    {
        return _nodeMap.Values
            .Where(n => n.Row == row)
            .OrderBy(n => n.Col)
            .ToList();
    }

    private void EnsureNode(MapPoint point, ActMap map, RunState? runState,
        Dictionary<MapCoord, NMapPoint> pointDict)
    {
        if (_nodeMap.ContainsKey(point))
            return;

        var state = GetStateFromUI(point.coord, pointDict);
        _nodeMap[point] = CreateNode(point, state, map, runState);
    }

    private MapNode CreateNode(MapPoint point, MapPointState state, ActMap map, RunState? runState)
    {
        if (point.PointType == MapPointType.Boss)
        {
            var bossName = GetBossName(point, map, runState);
            if (bossName != null)
                return new BossMapNode(point, state, bossName);
        }

        return new MapNode(point, state);
    }

    private static string? GetBossName(MapPoint point, ActMap map, RunState? runState)
    {
        if (runState == null) return null;

        var act = runState.Act;
        if (act == null) return null;

        if (point == map.BossMapPoint)
            return act.BossEncounter?.Title?.GetFormattedText();

        if (point == map.SecondBossMapPoint)
            return act.SecondBossEncounter?.Title?.GetFormattedText();

        return null;
    }

    private static MapPointState GetStateFromUI(MapCoord coord, Dictionary<MapCoord, NMapPoint> pointDict)
    {
        if (pointDict.TryGetValue(coord, out var nMapPoint))
            return nMapPoint.State;
        return MapPointState.None;
    }
}
