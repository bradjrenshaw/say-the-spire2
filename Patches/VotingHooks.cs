using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Localization;

namespace SayTheSpire2.Patches;

public static class VotingHooks
{
    private static readonly FieldInfo? MapPointDictField =
        AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");

    public static void Initialize(Harmony harmony)
    {
        // Map voting: remote player vote changed
        PatchIfFound(harmony, typeof(NMapScreen), "OnPlayerVoteChanged",
            nameof(MapVoteChangedPostfix), "Map OnPlayerVoteChanged");

        // Map voting: local player selected a point
        PatchIfFound(harmony, typeof(NMapScreen), "OnMapPointSelectedLocally",
            nameof(MapPointSelectedLocallyPostfix), "Map OnMapPointSelectedLocally");

        // Map voting: travel begins (destination chosen)
        PatchIfFound(harmony, typeof(NMapScreen), "TravelToMapCoord",
            nameof(TravelToMapCoordPrefix), "Map TravelToMapCoord", isPrefix: true);

        // Event voting: player vote changed (shared events)
        PatchIfFound(harmony, typeof(NEventLayout), "OnPlayerVoteChanged",
            nameof(EventVoteChangedPostfix), "Event OnPlayerVoteChanged");

        // Event voting: shared option chosen (result)
        PatchIfFound(harmony, typeof(NEventLayout), "BeforeSharedOptionChosen",
            nameof(SharedOptionChosenPrefix), "Event BeforeSharedOptionChosen", isPrefix: true);
    }

    // --- Map voting hooks ---

    public static void MapVoteChangedPostfix(NMapScreen __instance, Player player, MapVote? oldLocation, MapVote? newLocation)
    {
        try
        {
            if (!IsMultiplayer()) return;
            if (IsLocalPlayer(player)) return;

            if (newLocation == null) return;

            var playerName = GetPlayerName(player);
            var point = ResolveMapPoint(__instance, newLocation.Value.coord);
            var nodeName = GetMapPointName(point);
            EventDispatcher.Enqueue(new MapVoteEvent($"{playerName} voted for {nodeName}"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] MapVoteChanged error: {e.Message}");
        }
    }

    public static void MapPointSelectedLocallyPostfix(NMapScreen __instance, NMapPoint point)
    {
        try
        {
            if (!IsMultiplayer()) return;

            var nodeName = GetMapPointName(point);
            EventDispatcher.Enqueue(new MapVoteEvent($"Voted for {nodeName}"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] MapPointSelectedLocally error: {e.Message}");
        }
    }

    public static void TravelToMapCoordPrefix(NMapScreen __instance, MapCoord coord)
    {
        try
        {
            var point = ResolveMapPoint(__instance, coord);
            var nodeName = GetMapPointName(point);
            EventDispatcher.Enqueue(new MapVoteEvent($"Traveling to {nodeName}"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] TravelToMapCoord error: {e.Message}");
        }
    }

    // --- Event voting hooks ---

    public static void EventVoteChangedPostfix(NEventLayout __instance, Player player)
    {
        try
        {
            if (!IsMultiplayer()) return;
            if (IsLocalPlayer(player)) return;

            var voteIndex = RunManager.Instance.EventSynchronizer.GetPlayerVote(player);
            if (voteIndex == null) return;

            string? optionTitle = null;
            int i = 0;
            foreach (var button in __instance.OptionButtons)
            {
                if (i == (int)voteIndex.Value)
                {
                    optionTitle = button.Option?.Title?.GetFormattedText();
                    break;
                }
                i++;
            }

            var playerName = GetPlayerName(player);
            var title = optionTitle ?? $"option {voteIndex.Value + 1}";
            EventDispatcher.Enqueue(new EventVoteEvent($"{playerName} voted for {title}"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] EventVoteChanged error: {e.Message}");
        }
    }

    public static void SharedOptionChosenPrefix(NEventLayout __instance, EventOption option)
    {
        try
        {
            var title = option.Title?.GetFormattedText();
            if (!string.IsNullOrEmpty(title))
                EventDispatcher.Enqueue(new EventVoteEvent($"{title} chosen"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] SharedOptionChosen error: {e.Message}");
        }
    }

    // --- Helpers ---

    private static bool IsMultiplayer()
    {
        try { return RunManager.Instance.NetService.Type.IsMultiplayer(); }
        catch { return false; }
    }

    private static bool IsLocalPlayer(Player player)
    {
        return LocalContext.NetId.HasValue && player.NetId == LocalContext.NetId.Value;
    }

    private static string GetPlayerName(Player player)
    {
        try
        {
            var platform = RunManager.Instance.NetService.Platform;
            return PlatformUtil.GetPlayerName(platform, player.NetId);
        }
        catch { return $"Player {player.NetId}"; }
    }

    private static NMapPoint? ResolveMapPoint(NMapScreen screen, MapCoord coord)
    {
        if (MapPointDictField == null) return null;
        var dict = MapPointDictField.GetValue(screen) as Dictionary<MapCoord, NMapPoint>;
        if (dict != null && dict.TryGetValue(coord, out var point))
            return point;
        return null;
    }

    private static string GetMapPointName(NMapPoint? point)
    {
        if (point?.Point == null) return "Unknown";
        var typeKey = point.Point.PointType switch
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
        return LocalizationManager.GetOrDefault("map_nav", typeKey, point.Point.PointType.ToString());
    }

    private static void PatchIfFound(Harmony harmony, Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        var method = AccessTools.Method(type, methodName);
        if (method == null)
        {
            Log.Error($"[AccessibilityMod] Could not find {type.Name}.{methodName} for {label}!");
            return;
        }

        var handler = new HarmonyMethod(typeof(VotingHooks), handlerName);
        if (isPrefix)
            harmony.Patch(method, prefix: handler);
        else
            harmony.Patch(method, postfix: handler);
        Log.Info($"[AccessibilityMod] {label} hook patched.");
    }
}
