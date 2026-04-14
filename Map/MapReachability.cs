using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace SayTheSpire2.Map;

public static class MapReachability
{
    private static readonly System.Type? FlightType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.Modifiers.Flight");

    private static readonly System.Type? WingedBootsType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.Relics.WingedBoots");

    private static readonly System.Reflection.MethodInfo? ShouldAllowFreeTravelMethod =
        AccessTools.Method(typeof(AbstractModel), "ShouldAllowFreeTravel");

    public static MapReachabilityContext CreateContext(RunState? runState)
    {
        if (runState == null)
            return default;

        var hasPermanentFreeTravel = false;
        var remainingFreeTravelCharges = 0;

        try
        {
            if (ShouldAllowFreeTravelMethod == null)
                return default;

            foreach (AbstractModel listener in runState.IterateHookListeners(null))
            {
                if (ShouldAllowFreeTravelMethod.Invoke(listener, null) is not true)
                    continue;

                if (WingedBootsType != null && WingedBootsType.IsInstanceOfType(listener))
                {
                    if (GetIntProperty(listener, "DisplayAmount") is int remainingCharges)
                        remainingFreeTravelCharges = System.Math.Max(remainingFreeTravelCharges, remainingCharges);
                    continue;
                }

                if (FlightType == null || FlightType.IsInstanceOfType(listener))
                {
                    hasPermanentFreeTravel = true;
                    continue;
                }

                // The game exposes free travel generically as a bool hook, but it does not
                // expose a generic remaining-charge contract. Unknown free-travel sources are
                // treated as permanent so reachability does not underreport valid routes.
                hasPermanentFreeTravel = true;
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] MapReachability: context detection failed: {e.Message}");
        }

        return new MapReachabilityContext(hasPermanentFreeTravel, remainingFreeTravelCharges);
    }

    public static List<MapNode> GetForwardNodes(MapNode origin, MapHandler handler, MapReachabilityContext context)
    {
        if (context.CanUseFreeTravel)
        {
            var nextRow = handler.GetNodesAtRow(origin.Row + 1);
            if (nextRow.Count > 0)
                return nextRow;
        }

        return origin.ForwardEdges
            .Select(edge => edge.To)
            .OrderBy(child => child.Col)
            .ToList();
    }

    public static bool IsFreeTravelOnly(MapNode origin, MapNode target, MapHandler handler, MapReachabilityContext context)
    {
        if (!context.CanUseFreeTravel)
            return false;

        if (target.Row != origin.Row + 1)
            return false;

        if (!handler.GetNodesAtRow(origin.Row + 1).Any(candidate => candidate.Point.coord.Equals(target.Point.coord)))
            return false;

        return !origin.Point.Children.Contains(target.Point);
    }

    public static bool TryAdvance(MapNode origin, MapNode target, MapHandler handler,
        MapReachabilityContext context, out MapReachabilityContext nextContext)
    {
        if (origin.Point.Children.Contains(target.Point))
        {
            nextContext = context;
            return true;
        }

        if (IsFreeTravelOnly(origin, target, handler, context))
        {
            nextContext = context.ConsumeFreeTravel();
            return true;
        }

        nextContext = context;
        return false;
    }

    public static List<MapNode> GetReachableNodes(MapNode start, MapHandler handler, MapReachabilityContext context)
    {
        var bestContexts = GetBestContexts(start, handler, context);
        return bestContexts.Keys
            .Where(coord => !coord.Equals(start.Point.coord))
            .Select(handler.GetNode)
            .OfType<MapNode>()
            .OrderBy(node => node.Row)
            .ThenBy(node => node.Col)
            .ToList();
    }

    public static HashSet<MapCoord> GetReachableMarkedCoords(MapNode start, MapHandler handler,
        MapReachabilityContext context, HashSet<MapCoord> markedCoords)
    {
        var bestContexts = GetBestContexts(start, handler, context);
        return bestContexts.Keys
            .Where(markedCoords.Contains)
            .ToHashSet();
    }

    public static bool TryGetBestContextAtNode(MapNode start, MapHandler handler, MapReachabilityContext startContext,
        MapNode target, out MapReachabilityContext bestContext)
    {
        var bestContexts = GetBestContexts(start, handler, startContext);
        return bestContexts.TryGetValue(target.Point.coord, out bestContext);
    }

    private static Dictionary<MapCoord, MapReachabilityContext> GetBestContexts(MapNode start, MapHandler handler,
        MapReachabilityContext startContext)
    {
        var bestContexts = new Dictionary<MapCoord, MapReachabilityContext>
        {
            [start.Point.coord] = startContext
        };
        var stack = new Stack<(MapNode Node, MapReachabilityContext Context)>();
        stack.Push((start, startContext));

        while (stack.Count > 0)
        {
            var (node, context) = stack.Pop();

            foreach (var next in GetForwardNodes(node, handler, context))
            {
                if (!TryAdvance(node, next, handler, context, out var nextContext))
                    continue;

                if (bestContexts.TryGetValue(next.Point.coord, out var bestContext)
                    && !nextContext.BetterThan(bestContext))
                    continue;

                bestContexts[next.Point.coord] = nextContext;
                stack.Push((next, nextContext));
            }
        }

        return bestContexts;
    }

    private static int? GetIntProperty(object instance, string propertyName)
    {
        try
        {
            return instance.GetType().GetProperty(propertyName)?.GetValue(instance) is int value ? value : null;
        }
        catch
        {
            return null;
        }
    }
}
