using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Speech;

namespace SayTheSpire2.Patches;

/// <summary>
/// Harmony hooks for multiplayer features beyond voting:
/// - Act transition ready-up
/// - Network timeout/problem indicators
/// - Waiting for players overlay
/// </summary>
public static class MultiplayerHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(ActChangeSynchronizer), "OnPlayerReady",
            typeof(MultiplayerHooks), nameof(ActPlayerReadyPostfix), "Act OnPlayerReady");
        HarmonyHelper.PatchIfFound(harmony, typeof(NRewardsScreen), "HideWaitingForPlayersScreen",
            typeof(MultiplayerHooks), nameof(HideWaitingPostfix), "Rewards HideWaitingForPlayers");
        HarmonyHelper.PatchIfFound(harmony, typeof(NRewardsScreen), "OnProceedButtonPressed",
            typeof(MultiplayerHooks), nameof(ProceedButtonPostfix), "Rewards OnProceedButtonPressed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerTimeoutOverlay), "UpdateLoop",
            typeof(MultiplayerHooks), nameof(TimeoutUpdatePostfix), "Timeout UpdateLoop");
    }

    public static void ActPlayerReadyPostfix(Player player)
    {
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;
            if (MultiplayerHelper.IsLocalPlayer(player)) return;

            var name = MultiplayerHelper.GetPlayerName(player);
            SpeechManager.Output(Message.Localized("ui", "SPEECH.PLAYER_READY_PROCEED", new { name }));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] ActPlayerReady error: {e.Message}");
        }
    }

    public static void HideWaitingPostfix()
    {
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;
            SpeechManager.Output(Message.Localized("ui", "SPEECH.ALL_PLAYERS_READY"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] HideWaiting error: {e.Message}");
        }
    }

    public static void ProceedButtonPostfix(NRewardsScreen __instance)
    {
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;
            if (RunManager.Instance.ActChangeSynchronizer.IsWaitingForOtherPlayers())
                SpeechManager.Output(Message.Localized("ui", "SPEECH.WAITING_FOR_PLAYERS"));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] ProceedButton error: {e.Message}");
        }
    }

    private static bool _lastTimeoutShown;

    public static void TimeoutUpdatePostfix(NMultiplayerTimeoutOverlay __instance)
    {
        try
        {
            bool shown = __instance.IsShown;
            if (shown != _lastTimeoutShown)
            {
                _lastTimeoutShown = shown;
                if (shown)
                    SpeechManager.Output(Message.Localized("ui", "SPEECH.NO_RESPONSE_FROM_HOST"));
                else
                    SpeechManager.Output(Message.Localized("ui", "SPEECH.CONNECTION_RESTORED"));
            }
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] TimeoutUpdate error: {e.Message}");
        }
    }

}
