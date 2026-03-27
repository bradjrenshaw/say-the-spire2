using System.Collections.Generic;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace SayTheSpire2.Map;

public static class MapMarkerState
{
    private static readonly HashSet<MapCoord> MarkedCoords = new();
    private static RunState? _runState;
    private static ActMap? _map;

    public static IReadOnlyCollection<MapCoord> GetMarkedCoords()
    {
        EnsureCurrentRun();
        return MarkedCoords;
    }

    public static bool HasAnyMarkers()
    {
        EnsureCurrentRun();
        return MarkedCoords.Count > 0;
    }

    public static bool IsMarked(MapPoint point)
    {
        EnsureCurrentRun();
        return MarkedCoords.Contains(point.coord);
    }

    public static bool Mark(MapPoint point)
    {
        EnsureCurrentRun();
        return MarkedCoords.Add(point.coord);
    }

    public static bool Clear(MapPoint point)
    {
        EnsureCurrentRun();
        return MarkedCoords.Remove(point.coord);
    }

    public static void ClearAll()
    {
        EnsureCurrentRun();
        MarkedCoords.Clear();
    }

    private static void EnsureCurrentRun()
    {
        RunState? currentRunState = null;
        ActMap? currentMap = null;
        try
        {
            currentRunState = RunManager.Instance.DebugOnlyGetState();
            currentMap = currentRunState?.Map;
        }
        catch
        {
            // Ignore and treat as no active run.
        }

        if (!ReferenceEquals(_runState, currentRunState) || !ReferenceEquals(_map, currentMap))
        {
            _runState = currentRunState;
            _map = currentMap;
            MarkedCoords.Clear();
        }
    }
}
