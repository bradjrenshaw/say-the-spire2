using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.Views;

namespace SayTheSpire2.Patches;

/// <summary>
/// Hooks for the Dread lobby (November beta; resolved by name and skipped on
/// stable). The lobby itself is wired up with the other character select
/// screens in <see cref="LobbyHooks"/>.
///
/// Coming back to the lobby after a Dread run, the game plays the result on
/// the win-streak chain: a new link is forged after a win, or the chain
/// shatters after a loss. Nothing is written anywhere, so it is spoken here.
/// </summary>
public static class DreadHooks
{
    public static void Initialize(Harmony harmony)
    {
        if (AccessTools.TypeByName(DreadLobbyView.ScreenTypeName) is { } dreadScreen)
            HarmonyHelper.PatchIfFound(harmony, dreadScreen, "PlayDreadWinStreakResultAnimation",
                typeof(DreadHooks), nameof(StreakResultPrefix), "Dread PlayDreadWinStreakResultAnimation", isPrefix: true);
    }

    // winStreakResult is the game's PendingDreadWinStreakResult (beta-only
    // type), boxed. currentStreak is the streak after the result was applied.
    public static void StreakResultPrefix(int currentStreak, object winStreakResult)
    {
        try
        {
            var type = winStreakResult.GetType();
            var wasReset = AccessTools.Property(type, "WasReset")!.GetValue(winStreakResult) is true;
            var before = AccessTools.Property(type, "WinStreakBeforeReset")!.GetValue(winStreakResult) is int b ? b : 0;
            var increase = AccessTools.Property(type, "WinStreakIncrease")!.GetValue(winStreakResult) is int i ? i : 0;

            if (wasReset)
                SpeechManager.Output(Message.Localized("ui", "DREAD.STREAK_BROKEN", new { count = before }));
            else if (increase > 0)
                SpeechManager.Output(Message.Localized("ui", "DREAD.STREAK_INCREASED", new { count = currentStreak }));
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Dread streak result hook error: {e.Message}");
        }
    }
}
