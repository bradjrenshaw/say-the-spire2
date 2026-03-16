using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Runs;
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
        PatchIfFound(harmony, typeof(ActChangeSynchronizer), "OnPlayerReady",
            nameof(ActPlayerReadyPostfix), "Act OnPlayerReady");
        PatchIfFound(harmony, typeof(NRewardsScreen), "HideWaitingForPlayersScreen",
            nameof(HideWaitingPostfix), "Rewards HideWaitingForPlayers");
        PatchIfFound(harmony, typeof(NRewardsScreen), "OnProceedButtonPressed",
            nameof(ProceedButtonPostfix), "Rewards OnProceedButtonPressed");
        PatchIfFound(harmony, typeof(NMultiplayerTimeoutOverlay), "UpdateLoop",
            nameof(TimeoutUpdatePostfix), "Timeout UpdateLoop");
    }

    public static void ActPlayerReadyPostfix(Player player)
    {
        try
        {
            if (!MultiplayerHelper.IsMultiplayer()) return;
            if (MultiplayerHelper.IsLocalPlayer(player)) return;

            var name = MultiplayerHelper.GetPlayerName(player);
            SpeechManager.Output($"{name} is ready to proceed");
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
            SpeechManager.Output("All players ready");
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
                SpeechManager.Output("Waiting for other players");
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
                    SpeechManager.Output("Warning: No response from host");
                else
                    SpeechManager.Output("Connection restored");
            }
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] TimeoutUpdate error: {e.Message}");
        }
    }

    private static void PatchIfFound(Harmony harmony, Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(MultiplayerHooks), handlerName, label, isPrefix);
    }
}
