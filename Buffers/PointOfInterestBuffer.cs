using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using SayTheSpire2.Localization;
using SayTheSpire2.Map;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Buffers;

public sealed class PointOfInterestBuffer
{
    public enum PointOfInterestMode
    {
        Reachable,
        All,
    }

    private readonly Func<MapPoint?> _currentPointProvider;
    private readonly Func<MapPoint?> _anchorPointProvider;
    private readonly MapHandler _handler;

    private readonly List<MapNode> _nodes = new();
    private MapCoord? _selectedCoord;
    private MapCoord? _lastCurrentCoord;

    public PointOfInterestMode Mode { get; private set; } = PointOfInterestMode.Reachable;
    public MapNode? SelectedNode
    {
        get
        {
            var index = FindSelectedIndex();
            return index >= 0 && index < _nodes.Count ? _nodes[index] : null;
        }
    }

    public PointOfInterestBuffer(MapHandler handler, Func<MapPoint?> currentPointProvider, Func<MapPoint?> anchorPointProvider)
    {
        _handler = handler;
        _currentPointProvider = currentPointProvider;
        _anchorPointProvider = anchorPointProvider;
    }

    public void SetSelection(MapPoint? point)
    {
        _selectedCoord = point?.coord;
    }

    public bool MoveNext()
    {
        if (!Refresh())
            return false;

        if (_nodes.Count == 0)
            return false;

        int targetIndex;
        if (_selectedCoord.HasValue)
        {
            var currentIndex = FindSelectedIndex();
            if (currentIndex < 0 || currentIndex >= _nodes.Count - 1)
                return false;

            targetIndex = currentIndex + 1;
        }
        else
        {
            targetIndex = GetAnchorNextIndex();
            if (targetIndex < 0 || targetIndex >= _nodes.Count)
                return false;
        }

        SelectIndex(targetIndex);
        return true;
    }

    public bool MovePrevious()
    {
        if (!Refresh())
            return false;

        if (_nodes.Count == 0)
            return false;

        int targetIndex;
        if (_selectedCoord.HasValue)
        {
            var currentIndex = FindSelectedIndex();
            if (currentIndex <= 0)
                return false;

            targetIndex = currentIndex - 1;
        }
        else
        {
            targetIndex = GetAnchorPreviousIndex();
            if (targetIndex < 0 || targetIndex >= _nodes.Count)
                return false;
        }

        SelectIndex(targetIndex);
        return true;
    }

    public string ToggleMode()
    {
        Mode = Mode == PointOfInterestMode.Reachable
            ? PointOfInterestMode.All
            : PointOfInterestMode.Reachable;

        _selectedCoord = null;
        Refresh();

        var modeLabel = GetModeLabel();
        var targetIndex = _nodes.Count > 0 ? 0 : -1;
        if (targetIndex < 0 || targetIndex >= _nodes.Count)
            return modeLabel;

        SelectIndex(targetIndex);
        return modeLabel;
    }

    private bool Refresh()
    {
        _nodes.Clear();

        var currentPoint = _currentPointProvider();
        if (currentPoint == null)
        {
            _selectedCoord = null;
            _lastCurrentCoord = null;
            return false;
        }

        var currentCoord = currentPoint.coord;
        if (!_lastCurrentCoord.HasValue || !_lastCurrentCoord.Value.Equals(currentCoord))
            _selectedCoord = null;
        _lastCurrentCoord = currentCoord;

        var currentNode = _handler.GetNode(currentPoint);
        if (currentNode == null)
        {
            _selectedCoord = null;
            return false;
        }

        _nodes.AddRange(Mode == PointOfInterestMode.Reachable
            ? BuildReachableNodes(currentNode)
            : BuildAllNodes(currentNode));

        if (_selectedCoord.HasValue && FindSelectedIndex() < 0)
            _selectedCoord = null;

        return true;
    }

    private List<MapNode> BuildReachableNodes(MapNode currentNode)
    {
        var reachable = MapReachability.GetReachableNodes(currentNode, _handler, _handler.ReachabilityContext);
        return SortByVisualOrder(reachable.Where(MatchesEnabledCategories));
    }

    private List<MapNode> BuildAllNodes(MapNode currentNode)
    {
        return SortByVisualOrder(_handler.Nodes.Values
            .Where(node => !node.Point.coord.Equals(currentNode.Point.coord))
            .Where(MatchesEnabledCategories));
    }

    private bool MatchesEnabledCategories(MapNode node)
    {
        var point = node.Point;

        if (point.Quests.Count > 0 && ModSettings.GetValue<bool>("map.points_of_interest.quest_marked"))
            return true;

        return point.PointType switch
        {
            MapPointType.Elite => ModSettings.GetValue<bool>("map.points_of_interest.elite"),
            MapPointType.Shop => ModSettings.GetValue<bool>("map.points_of_interest.shop"),
            MapPointType.Treasure => ModSettings.GetValue<bool>("map.points_of_interest.treasure"),
            MapPointType.RestSite => ModSettings.GetValue<bool>("map.points_of_interest.rest_site"),
            MapPointType.Unknown => ModSettings.GetValue<bool>("map.points_of_interest.unknown"),
            MapPointType.Monster => ModSettings.GetValue<bool>("map.points_of_interest.monster"),
            MapPointType.Boss => ModSettings.GetValue<bool>("map.points_of_interest.boss"),
            MapPointType.Ancient => ModSettings.GetValue<bool>("map.points_of_interest.ancient"),
            _ => false,
        };
    }

    private int GetAnchorNextIndex()
    {
        if (_nodes.Count == 0)
            return -1;

        if (Mode == PointOfInterestMode.Reachable)
            return 0;

        var anchorPoint = _anchorPointProvider() ?? _currentPointProvider();
        if (anchorPoint == null)
            return 0;

        var currentNode = _handler.GetNode(anchorPoint);
        if (currentNode == null)
            return 0;

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (CompareVisualOrder(_nodes[i], currentNode) > 0)
                return i;
        }

        return -1;
    }

    private int GetAnchorPreviousIndex()
    {
        if (_nodes.Count == 0)
            return -1;

        if (Mode == PointOfInterestMode.Reachable)
            return -1;

        var anchorPoint = _anchorPointProvider() ?? _currentPointProvider();
        if (anchorPoint == null)
            return _nodes.Count - 1;

        var currentNode = _handler.GetNode(anchorPoint);
        if (currentNode == null)
            return _nodes.Count - 1;

        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            if (CompareVisualOrder(_nodes[i], currentNode) < 0)
                return i;
        }

        return -1;
    }

    private int FindSelectedIndex()
    {
        if (!_selectedCoord.HasValue)
            return -1;

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i].Point.coord.Equals(_selectedCoord.Value))
                return i;
        }

        return -1;
    }

    private void SelectIndex(int index)
    {
        _selectedCoord = _nodes[index].Point.coord;
    }

    private static List<MapNode> SortByVisualOrder(IEnumerable<MapNode> nodes)
    {
        return nodes.OrderBy(node => node.Row)
            .ThenBy(node => node.Col)
            .ToList();
    }

    private static int CompareVisualOrder(MapNode left, MapNode right)
    {
        var row = left.Row.CompareTo(right.Row);
        if (row != 0)
            return row;

        return left.Col.CompareTo(right.Col);
    }

    private string GetModeLabel()
    {
        return Mode == PointOfInterestMode.Reachable
            ? Ui("MAP_POI.MODE_REACHABLE")
            : Ui("MAP_POI.MODE_ALL");
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
