using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace SayTheSpire2.Map;

public class MapHandler
{
    private static readonly FieldInfo? MapField =
        AccessTools.Field(typeof(NMapScreen), "_map");
    private static readonly FieldInfo? RunStateField =
        AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly FieldInfo? MapPointDictField =
        AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");

    private readonly Dictionary<MapPoint, MapNode> _nodeMap = new();
    private readonly List<MapEdge> _edges = new();
    private bool _allowsFreeTravel;
    private MapPoint? _currentPoint;

    public IReadOnlyDictionary<MapPoint, MapNode> Nodes => _nodeMap;
    public IReadOnlyList<MapEdge> Edges => _edges;
    public bool AllowsFreeTravel => _allowsFreeTravel;

    public bool Build()
    {
        _nodeMap.Clear();
        _edges.Clear();
        _allowsFreeTravel = false;
        _currentPoint = null;

        var screen = NMapScreen.Instance;
        if (screen == null)
        {
            Log.Error("[AccessibilityMod] MapHandler: NMapScreen.Instance is null");
            return false;
        }

        var map = MapField?.GetValue(screen) as ActMap;
        var runState = RunStateField?.GetValue(screen) as RunState;
        var pointDict = MapPointDictField?.GetValue(screen) as Dictionary<MapCoord, NMapPoint>;
        _allowsFreeTravel = ShouldAllowFreeTravel(runState);
        _currentPoint = runState?.CurrentMapPoint;

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

        // Build the static map graph from the authored child paths first.
        foreach (var (mapPoint, node) in _nodeMap)
        {
            foreach (var child in mapPoint.Children)
            {
                if (_nodeMap.TryGetValue(child, out var childNode))
                    AddEdge(node, childNode);
            }
        }

        AddDynamicTravelEdges(runState);

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

    public bool IsFreeTravelOnlyFromCurrent(MapNode node)
    {
        return IsFreeTravelOnlyFrom(node, _currentPoint);
    }

    public bool IsFreeTravelOnlyFrom(MapNode node, MapPoint? originPoint)
    {
        if (!_allowsFreeTravel)
            return false;

        if (originPoint == null)
            return false;

        if (node.Row != originPoint.coord.row + 1)
            return false;

        return !originPoint.Children.Contains(node.Point);
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

    private void AddDynamicTravelEdges(RunState? runState)
    {
        var currentPoint = runState?.CurrentMapPoint;
        if (currentPoint == null)
            return;

        var currentNode = GetNode(currentPoint);
        if (currentNode == null)
            return;

        var nextRow = currentNode.Row + 1;
        foreach (var candidate in GetNodesAtRow(nextRow))
        {
            if (candidate.State != MapPointState.Travelable)
                continue;

            AddEdge(currentNode, candidate);
        }
    }

    private static readonly MethodInfo? ShouldAllowFreeTravelMethod =
        AccessTools.Method("MegaCrit.Sts2.Core.Hooks.Hook:ShouldAllowFreeTravel");

    private static bool ShouldAllowFreeTravel(RunState? runState)
    {
        if (runState == null)
            return false;

        try
        {
            if (ShouldAllowFreeTravelMethod?.Invoke(null, new object[] { runState }) is bool allowed)
                return allowed;

            var modifiersProperty = runState.GetType().GetProperty("Modifiers");
            if (modifiersProperty?.GetValue(runState) is System.Collections.IEnumerable modifiers)
            {
                foreach (var modifier in modifiers)
                {
                    if (modifier?.GetType().GetMethod("ShouldAllowFreeTravel")?.Invoke(modifier, null) is bool modifierAllows
                        && modifierAllows)
                        return true;
                }
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] MapHandler: free-travel detection failed: {e.Message}");
        }

        return false;
    }

    private void AddEdge(MapNode from, MapNode to)
    {
        if (from.ForwardEdges.Any(edge => edge.To == to))
            return;

        var edge = new MapEdge(from, to);
        _edges.Add(edge);
        from.ForwardEdges.Add(edge);
        to.BackwardEdges.Add(edge);
    }
}
