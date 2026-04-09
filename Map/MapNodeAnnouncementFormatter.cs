using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.Map;

public static class MapNodeAnnouncementFormatter
{
    public static string DescribeNode(MapNode node, MapHandler handler, IReadOnlyList<MapNode>? rowNodes = null,
        bool includeChoicePrefix = false)
    {
        rowNodes ??= GetDefaultRowNodes(node);

        var type = node.GetDisplayName();
        if (MapMarkerState.IsMarked(node.Point))
            type = $"{GetMarkedText()}, {type}";

        string announcement;
        var state = node.GetStateString();
        if (state != null)
        {
            announcement = Message.Localized("map_nav", "NAV.NODE_WITH_STATE", new
            {
                type,
                coordinates = node.GetCoordinatesString(),
                state
            }).Resolve();
        }
        else
        {
            announcement = Message.Localized("map_nav", "NAV.NODE", new
            {
                type,
                coordinates = node.GetCoordinatesString()
            }).Resolve();
        }

        var guidance = BuildMarkerGuidance(node, handler, rowNodes);
        if (guidance.Count > 0)
            announcement = $"{announcement}, {string.Join(", ", guidance)}";

        var voteSummary = BuildVoteSummary(node);
        if (!string.IsNullOrEmpty(voteSummary))
            announcement = $"{announcement}, {voteSummary}";

        if (includeChoicePrefix && rowNodes.Count > 1)
            announcement = $"{GetChoiceText()}, {announcement}";

        return announcement;
    }

    public static List<MapNode> GetDefaultRowNodes(MapNode node)
    {
        MapNode? parent = node.BackwardEdges
            .FirstOrDefault(edge => edge.From.State == MegaCrit.Sts2.Core.Map.MapPointState.Traveled)
            ?.From
            ?? node.BackwardEdges.FirstOrDefault()?.From;

        if (parent == null)
            return new List<MapNode> { node };

        return parent.ForwardEdges
            .Select(edge => edge.To)
            .OrderBy(child => child.Col)
            .ToList();
    }

    private static List<string> BuildMarkerGuidance(MapNode node, MapHandler handler, IReadOnlyList<MapNode> rowNodes)
    {
        var markedCoords = new HashSet<MegaCrit.Sts2.Core.Map.MapCoord>(MapMarkerState.GetMarkedCoords());
        if (markedCoords.Count == 0)
            return new List<string>();

        var currentReachable = GetReachableMarkedCoords(node, markedCoords);
        currentReachable.Remove(node.Point.coord);

        var alternativeReachable = new HashSet<MegaCrit.Sts2.Core.Map.MapCoord>();
        if (rowNodes.Count > 1)
        {
            foreach (var sibling in rowNodes)
            {
                if (sibling.Point.coord.Equals(node.Point.coord))
                    continue;

                alternativeReachable.UnionWith(GetReachableMarkedCoords(sibling, markedCoords));
            }
        }

        var onPathNodes = SortByVisualOrder(currentReachable
            .Select(handler.GetNode)
            .OfType<MapNode>());
        var divergesNodes = SortByVisualOrder(alternativeReachable
            .Where(coord => !currentReachable.Contains(coord))
            .Select(handler.GetNode)
            .OfType<MapNode>());

        if (onPathNodes.Count == 0 && divergesNodes.Count == 0)
            return new List<string>();

        var duplicateNames = GetDuplicateNames(onPathNodes.Concat(divergesNodes));
        var guidance = new List<string>();

        if (onPathNodes.Count > 0)
        {
            guidance.Add(Message.Localized("map_nav", "NAV.ON_PATH_TO", new
            {
                markers = string.Join(", ", onPathNodes.Select(marker => GetMarkerLabel(marker, duplicateNames)))
            }).Resolve());
        }

        if (divergesNodes.Count > 0)
        {
            guidance.Add(Message.Localized("map_nav", "NAV.DIVERGES_FROM", new
            {
                markers = string.Join(", ", divergesNodes.Select(marker => GetMarkerLabel(marker, duplicateNames)))
            }).Resolve());
        }

        return guidance;
    }

    private static string? BuildVoteSummary(MapNode node)
    {
        if (!MultiplayerHelper.IsMultiplayer())
            return null;

        IReadOnlyList<MegaCrit.Sts2.Core.Entities.Players.Player>? players;
        try
        {
            players = RunManager.Instance.DebugOnlyGetState()?.Players;
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Failed to read map vote players: {e.Message}");
            return null;
        }

        if (players == null || players.Count == 0)
            return null;

        var voters = new List<string>();
        foreach (var player in players)
        {
            try
            {
                var vote = RunManager.Instance.MapSelectionSynchronizer.GetVote(player);
                if (vote?.coord.Equals(node.Point.coord) == true)
                    voters.Add(MultiplayerHelper.GetPlayerName(player));
            }
            catch (System.Exception e)
            {
                Log.Info($"[AccessibilityMod] Failed to read map vote for {player.NetId}: {e.Message}");
            }
        }

        return voters.Count > 0
            ? Message.Localized("ui", "EVENT.VOTED_FOR_BY", new
            {
                players = string.Join(", ", voters)
            }).Resolve()
            : null;
    }

    private static HashSet<MegaCrit.Sts2.Core.Map.MapCoord> GetReachableMarkedCoords(MapNode start,
        HashSet<MegaCrit.Sts2.Core.Map.MapCoord> markedCoords)
    {
        var reachable = new HashSet<MegaCrit.Sts2.Core.Map.MapCoord>();
        var visited = new HashSet<MegaCrit.Sts2.Core.Map.MapCoord>();
        var stack = new Stack<MapNode>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node.Point.coord))
                continue;

            if (markedCoords.Contains(node.Point.coord))
                reachable.Add(node.Point.coord);

            foreach (var child in node.ForwardEdges.Select(edge => edge.To))
                stack.Push(child);
        }

        return reachable;
    }

    private static List<MapNode> SortByVisualOrder(IEnumerable<MapNode> nodes)
    {
        return nodes.OrderBy(node => node.Row)
            .ThenBy(node => node.Col)
            .ToList();
    }

    private static HashSet<string> GetDuplicateNames(IEnumerable<MapNode> nodes)
    {
        return nodes.GroupBy(node => node.GetDisplayName())
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
    }

    private static string GetMarkerLabel(MapNode node, HashSet<string> duplicateNames)
    {
        var name = node.GetDisplayName();
        return duplicateNames.Contains(name)
            ? $"{name} {node.GetCoordinatesString()}"
            : name;
    }

    private static string GetChoiceText()
    {
        return LocalizationManager.GetOrDefault("map_nav", "NAV.CHOICE", "choice");
    }

    private static string GetMarkedText()
    {
        return LocalizationManager.GetOrDefault("map_nav", "MARKERS.MARKED", "Marked");
    }
}
