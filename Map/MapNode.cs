using System.Collections.Generic;
using MegaCrit.Sts2.Core.Map;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Map;

public class MapNode
{
    public MapPoint Point { get; }
    public MapPointType PointType => Point.PointType;
    public MapPointState State { get; set; }
    public int Col => Point.coord.col;
    public int Row => Point.coord.row;

    public List<MapEdge> ForwardEdges { get; } = new();
    public List<MapEdge> BackwardEdges { get; } = new();

    public IEnumerable<MapNode> Children
    {
        get
        {
            foreach (var edge in ForwardEdges)
                yield return edge.To;
        }
    }

    public IEnumerable<MapNode> Parents
    {
        get
        {
            foreach (var edge in BackwardEdges)
                yield return edge.From;
        }
    }

    public MapNode(MapPoint point, MapPointState state)
    {
        Point = point;
        State = state;
    }

    public virtual string GetDisplayName()
    {
        var typeKey = PointType switch
        {
            MapPointType.Unknown => "NODE_TYPES.UNKNOWN",
            MapPointType.Shop => "NODE_TYPES.SHOP",
            MapPointType.Treasure => "NODE_TYPES.TREASURE",
            MapPointType.RestSite => "NODE_TYPES.REST_SITE",
            MapPointType.Monster => "NODE_TYPES.MONSTER",
            MapPointType.Elite => "NODE_TYPES.ELITE",
            MapPointType.Boss => "NODE_TYPES.BOSS",
            MapPointType.Ancient => "NODE_TYPES.ANCIENT",
            _ => "NODE_TYPES.UNKNOWN",
        };
        return LocalizationManager.GetOrDefault("map_nav", typeKey, PointType.ToString());
    }

    public string? GetStateString()
    {
        var stateKey = State switch
        {
            MapPointState.Travelable => "STATE.TRAVELABLE",
            MapPointState.Traveled => "STATE.TRAVELED",
            MapPointState.Untravelable => "STATE.UNTRAVELABLE",
            _ => null,
        };
        return stateKey != null ? LocalizationManager.Get("map_nav", stateKey) : null;
    }

    public string GetCoordinatesString()
    {
        return Message.Localized("map_nav", "NAV.COORDINATES", new { col = Col, row = Row }).Resolve();
    }
}
