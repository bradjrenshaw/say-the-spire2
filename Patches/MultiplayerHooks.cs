using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
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
        // Act ready-up: player ready to move on
        PatchIfFound(harmony, typeof(ActChangeSynchronizer), "OnPlayerReady",
            nameof(ActPlayerReadyPostfix), "Act OnPlayerReady");

        // Rewards screen: waiting overlay hidden (all players ready)
        PatchIfFound(harmony, typeof(NRewardsScreen), "HideWaitingForPlayersScreen",
            nameof(HideWaitingPostfix), "Rewards HideWaitingForPlayers");

        // Rewards screen: proceed button shows waiting overlay
        PatchIfFound(harmony, typeof(NRewardsScreen), "OnProceedButtonPressed",
            nameof(ProceedButtonPostfix), "Rewards OnProceedButtonPressed");

        // Network timeout overlay visibility
        PatchIfFound(harmony, typeof(NMultiplayerTimeoutOverlay), "UpdateLoop",
            nameof(TimeoutUpdatePostfix), "Timeout UpdateLoop");
    }

    // --- Act ready-up ---

    public static void ActPlayerReadyPostfix(Player player)
    {
        try
        {
            if (!IsMultiplayer()) return;
            if (IsLocalPlayer(player)) return;

            var name = GetPlayerName(player);
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
            if (!IsMultiplayer()) return;
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
            if (!IsMultiplayer()) return;
            if (RunManager.Instance.ActChangeSynchronizer.IsWaitingForOtherPlayers())
                SpeechManager.Output("Waiting for other players");
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] ProceedButton error: {e.Message}");
        }
    }

    // --- Network timeout ---

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

    private static void PatchIfFound(Harmony harmony, Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        var method = AccessTools.Method(type, methodName);
        if (method == null)
        {
            Log.Error($"[AccessibilityMod] Could not find {type.Name}.{methodName} for {label}!");
            return;
        }

        var handler = new HarmonyMethod(typeof(MultiplayerHooks), handlerName);
        if (isPrefix)
            harmony.Patch(method, prefix: handler);
        else
            harmony.Patch(method, postfix: handler);
        Log.Info($"[AccessibilityMod] {label} hook patched.");
    }
}
