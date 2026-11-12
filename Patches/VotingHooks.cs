using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.Localization;
using SayTheSpire2.Map;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.Patches;

public static class VotingHooks
{
    public readonly record struct LocalMapVoteState(bool ShouldAnnounce, string? NodeName, Creature? LocalCreature);

    private static readonly FieldInfo? MapPointDictField =
        AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");

    public static void Initialize(Harmony harmony)
    {
        // Map voting: remote player vote changed
        HarmonyHelper.PatchIfFound(harmony, typeof(NMapScreen), "OnPlayerVoteChanged",
            typeof(VotingHooks), nameof(MapVoteChangedPostfix), "Map OnPlayerVoteChanged");

        // Map voting: local player selected a point
        HarmonyHelper.PatchIfFound(harmony, typeof(NMapScreen), "OnMapPointSelectedLocally",
            typeof(VotingHooks), nameof(MapPointSelectedLocallyPrefix), "Map OnMapPointSelectedLocally", isPrefix: true);
        HarmonyHelper.PatchIfFound(harmony, typeof(NMapScreen), "OnMapPointSelectedLocally",
            typeof(VotingHooks), nameof(MapPointSelectedLocallyPostfix), "Map OnMapPointSelectedLocally Postfix");

        // Map voting: travel begins (destination chosen)
        HarmonyHelper.PatchIfFound(harmony, typeof(NMapScreen), "TravelToMapCoord",
            typeof(VotingHooks), nameof(TravelToMapCoordPrefix), "Map TravelToMapCoord", isPrefix: true);

        // Blitz (beta-only) travels through the top-bar map, not the map
        // screen. Resolved by name; absent on stable.
        if (AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.CommonUi.NBlitzTopBarMap") is { } blitzMap)
            HarmonyHelper.PatchIfFound(harmony, blitzMap, "TravelToMapCoord",
                typeof(VotingHooks), nameof(BlitzTravelPrefix), "BlitzMap TravelToMapCoord", isPrefix: true);

        // Event voting: player vote changed (shared events)
        HarmonyHelper.PatchIfFound(harmony, typeof(NEventLayout), "OnPlayerVoteChanged",
            typeof(VotingHooks), nameof(EventVoteChangedPostfix), "Event OnPlayerVoteChanged");

        // Event voting: shared option chosen (result)
        HarmonyHelper.PatchIfFound(harmony, typeof(NEventLayout), "BeforeSharedOptionChosen",
            typeof(VotingHooks), nameof(SharedOptionChosenPrefix), "Event BeforeSharedOptionChosen", isPrefix: true);

        // Shared relic voting: player vote changed
        HarmonyHelper.PatchIfFound(harmony, typeof(TreasureRoomRelicSynchronizer), "OnPicked",
            typeof(VotingHooks), nameof(RelicVotePickedPrefix), "Treasure Room Relic OnPicked", isPrefix: true);
    }

    public static void MapVoteChangedPostfix(NMapScreen __instance, Player player, MapVote? oldLocation, MapVote? newLocation)
    {
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;
            if (BlitzRun.IsActive)
            {
                AnnounceBlitzProceedVote(player, oldLocation, newLocation);
                return;
            }
            if (MultiplayerHelper.IsLocalPlayer(player)) return;
            if (newLocation == null) return;

            var playerName = MultiplayerHelper.GetPlayerName(player);
            var point = ResolveMapPoint(__instance, newLocation.Value.coord);
            var nodeName = GetMapPointName(point);
            EventDispatcher.Enqueue(new MapVoteEvent(playerName, nodeName, player.Creature));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] MapVoteChanged error: {e.Message}");
        }
    }

    // In Blitz there is one next room and no map screen: a player's map vote
    // is them pressing Proceed (the game shows it as their icon on the Proceed
    // button). The map screen still exists and still receives the vote, so
    // this arrives through the same hook. Only a fresh vote is announced —
    // the synchronizer re-fires the event when it reconciles a local vote.
    private static void AnnounceBlitzProceedVote(Player player, MapVote? oldLocation, MapVote? newLocation)
    {
        if (oldLocation != null || newLocation == null) return;

        if (!MultiplayerHelper.IsLocalPlayer(player))
        {
            EventDispatcher.Enqueue(new MapVoteEvent(MultiplayerHelper.GetPlayerName(player), "",
                player.Creature, MapVoteEvent.VoteKind.RemoteProceed));
            return;
        }

        // Our own Proceed: only worth saying when we're left waiting on
        // others. If ours was the last vote the party moves on at once.
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || WouldCompleteAllMapVotes(runState, player)) return;
        EventDispatcher.Enqueue(new MapVoteEvent("", "", player.Creature, MapVoteEvent.VoteKind.LocalProceed));
    }

    public static void BlitzTravelPrefix(MapCoord coord)
    {
        try
        {
            var point = RunManager.Instance.DebugOnlyGetState()?.Map.GetPoint(coord);
            if (point == null) return;
            // No coordinates: the act is a single line of rooms.
            EventDispatcher.Enqueue(new TravelEvent(Map.MapNode.GetPointTypeName(point.PointType)));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Blitz TravelToMapCoord error: {e.Message}");
        }
    }

    public static void MapPointSelectedLocallyPrefix(NMapScreen __instance, NMapPoint point, out LocalMapVoteState __state)
    {
        __state = default;
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return;

            var me = LocalContext.GetMe(runState);
            if (me == null) return;

            // Re-selecting the node we already voted for changes nothing. Read
            // from the synchronizer (as the game does): the November beta
            // removed NMapScreen.PlayerVoteDictionary, and GetVote exists on
            // both branches.
            var currentVote = RunManager.Instance.MapSelectionSynchronizer.GetVote(me);
            if (currentVote.HasValue && currentVote.Value.coord == point.Point.coord)
                return;

            if (WouldCompleteAllMapVotes(runState, me))
                return;

            var nodeName = GetMapPointName(point);
            Creature? localCreature = null;
            try
            {
                localCreature = me.Creature;
            }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Local creature lookup failed: {e.Message}");
            }

            __state = new LocalMapVoteState(true, nodeName, localCreature);
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] MapPointSelectedLocally prefix error: {e.Message}");
        }
    }

    public static void MapPointSelectedLocallyPostfix(LocalMapVoteState __state)
    {
        try
        {
            if (!__state.ShouldAnnounce || string.IsNullOrWhiteSpace(__state.NodeName))
                return;

            EventDispatcher.Enqueue(new MapVoteEvent("", __state.NodeName, __state.LocalCreature, MapVoteEvent.VoteKind.LocalVote));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] MapPointSelectedLocally postfix error: {e.Message}");
        }
    }

    public static void TravelToMapCoordPrefix(NMapScreen __instance, MapCoord coord)
    {
        try
        {
            var point = ResolveMapPoint(__instance, coord);
            var nodeName = GetMapPointName(point);
            EventDispatcher.Enqueue(new TravelEvent(nodeName));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] TravelToMapCoord error: {e.Message}");
        }
    }

    public static void RelicVotePickedPrefix(TreasureRoomRelicSynchronizer __instance, Player player, int? index)
    {
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;

            if (index.HasValue)
            {
                var title = GetRelicVoteTitle(__instance, index.Value);
                if (string.IsNullOrWhiteSpace(title))
                    return;

                if (MultiplayerHelper.IsLocalPlayer(player))
                {
                    EventDispatcher.Enqueue(new MapVoteEvent("", title, player.Creature, MapVoteEvent.VoteKind.LocalVote));
                }
                else
                {
                    EventDispatcher.Enqueue(new MapVoteEvent(MultiplayerHelper.GetPlayerName(player), title, player.Creature));
                }
                return;
            }

            if (MultiplayerHelper.IsLocalPlayer(player))
            {
                EventDispatcher.Enqueue(new MapVoteEvent("", "", player.Creature, MapVoteEvent.VoteKind.LocalSkip));
            }
            else
            {
                EventDispatcher.Enqueue(new MapVoteEvent(MultiplayerHelper.GetPlayerName(player), "", player.Creature, MapVoteEvent.VoteKind.RemoteSkip));
            }
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] RelicVotePicked error: {e.Message}");
        }
    }

    public static void EventVoteChangedPostfix(NEventLayout __instance, Player player)
    {
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;
            if (MultiplayerHelper.IsLocalPlayer(player)) return;

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

            var playerName = MultiplayerHelper.GetPlayerName(player);
            var title = optionTitle ?? $"option {voteIndex.Value + 1}";
            EventDispatcher.Enqueue(new EventVoteEvent(playerName, title, player.Creature));
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
                EventDispatcher.Enqueue(new EventVoteEvent("", title));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] SharedOptionChosen error: {e.Message}");
        }
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
        if (point?.Point == null) return LocalizationManager.GetOrDefault("ui", "LABELS.UNKNOWN", "Unknown");
        var name = Map.MapNode.GetPointTypeName(point.Point.PointType);
        var coords = Map.MapNode.GetCoordinates(point.Point).Resolve();
        return $"{name} {coords}";
    }

    private static string? GetRelicVoteTitle(TreasureRoomRelicSynchronizer synchronizer, int index)
    {
        var relics = synchronizer.CurrentRelics;
        if (relics == null || index < 0 || index >= relics.Count)
            return null;

        RelicModel relic = relics[index];
        return relic.Title.GetFormattedText();
    }

    private static bool WouldCompleteAllMapVotes(RunState runState, Player localPlayer)
    {
        foreach (var player in runState.Players)
        {
            if (player.NetId == localPlayer.NetId)
                continue;

            var vote = RunManager.Instance.MapSelectionSynchronizer.GetVote(player);
            if (!vote.HasValue || vote.Value.mapGenerationCount != RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount)
                return false;
        }

        return runState.Players.Count > 1;
    }
}
