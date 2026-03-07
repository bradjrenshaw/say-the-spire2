using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.Events;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class ScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        // Core screen context change hook
        var updateMethod = AccessTools.Method(typeof(ActiveScreenContext), "Update");
        if (updateMethod != null)
        {
            harmony.Patch(updateMethod,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(UpdatePostfix)));
            Log.Info("[AccessibilityMod] Screen hooks patched successfully.");
        }

        // Game over hooks
        PatchIfFound(harmony, typeof(NGameOverScreen), "InitializeBannerAndQuote",
            nameof(GameOverBannerPostfix), "GameOver banner");
        PatchIfFound(harmony, typeof(NGameOverScreen), "AddBadge",
            nameof(AddBadgePostfix), "GameOver AddBadge");
        PatchIfFound(harmony, typeof(NGameOverScreen), "AnimateScoreBar",
            nameof(AnimateScoreBarPrefix), "GameOver AnimateScoreBar", isPrefix: true);

        // Timeline hooks
        PatchIfFound(harmony, typeof(NTimelineScreen), "EnableInput",
            nameof(TimelineEnableInputPostfix), "Timeline EnableInput");

        // Unlock screen hooks
        PatchIfFound(harmony, typeof(NUnlockScreen), "Open",
            nameof(UnlockScreenOpenPostfix), "Unlock screen Open");

        // Epoch inspect hooks
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "Open",
            nameof(EpochInspectOpenPostfix), "Epoch inspect Open");
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "OpenViaPaginator",
            nameof(EpochPaginatePostfix), "Epoch paginate");
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "Close",
            nameof(EpochInspectClosedPostfix), "Epoch inspect Close");

        // Settings screen hooks (OnSubmenuOpened/Closed work for both main menu and pause)
        PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuOpened",
            nameof(SettingsOpenedPostfix), "Settings OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuClosed",
            nameof(SettingsClosedPostfix), "Settings OnSubmenuClosed");

        // Run lifecycle hooks
        PatchIfFound(harmony, typeof(RunManager), "Launch",
            nameof(RunLaunchPostfix), "Run Launch");
        PatchIfFound(harmony, typeof(RunManager), "OnEnded",
            nameof(RunEndedPostfix), "Run OnEnded");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        var method = AccessTools.Method(type, methodName);
        if (method == null) return;

        var handler = new HarmonyMethod(typeof(ScreenHooks), handlerName);
        if (isPrefix)
            harmony.Patch(method, prefix: handler);
        else
            harmony.Patch(method, postfix: handler);
        Log.Info($"[AccessibilityMod] {label} hook patched.");
    }

    // Core
    public static void UpdatePostfix() => ScreenManager.OnGameScreenChanged();

    // Game over delegates
    // Banner fires before ActiveScreenContext.Update(), so Current may not exist yet.
    // Ensure the screen is pushed early.
    public static void GameOverBannerPostfix(NGameOverScreen __instance)
    {
        if (GameOverScreen.Current == null)
            ScreenManager.PushScreen(new GameOverScreen());
        GameOverScreen.Current?.OnBannerAndQuote(__instance);
    }

    public static void AddBadgePostfix(string locEntryKey, string? locAmountKey, int amount)
        => GameOverScreen.Current?.OnBadge(locEntryKey, locAmountKey, amount);

    public static void AnimateScoreBarPrefix(NGameOverScreen __instance)
        => GameOverScreen.Current?.OnScore(__instance);

    // Timeline delegates
    public static void TimelineEnableInputPostfix()
        => TimelineGameScreen.Current?.OnEnableInput();

    public static void UnlockScreenOpenPostfix(NUnlockScreen __instance)
        => TimelineGameScreen.Current?.OnUnlockScreenOpen(__instance);

    // Epoch inspect delegates
    public static void EpochInspectOpenPostfix(EpochModel epoch, bool wasRevealed)
    {
        if (EpochInspectScreen.Current == null)
            ScreenManager.PushScreen(new EpochInspectScreen());
        EpochInspectScreen.Current?.OnOpen(epoch, wasRevealed);
    }

    public static void EpochPaginatePostfix(EpochModel epoch)
        => EpochInspectScreen.Current?.OnPaginate(epoch);

    public static void EpochInspectClosedPostfix()
    {
        if (EpochInspectScreen.Current != null)
            ScreenManager.RemoveScreen(EpochInspectScreen.Current);
    }

    // Settings delegates
    public static void SettingsOpenedPostfix(NSettingsScreen __instance)
    {
        // OnSubmenuOpened fires before ActiveScreenContext.Update(), so we push
        // here and flag it so OnGameScreenChanged doesn't interfere.
        if (SettingsGameScreen.Current == null)
        {
            var screen = new SettingsGameScreen(__instance);
            ScreenManager.PushScreen(screen);
        }
    }

    public static void SettingsClosedPostfix()
    {
        if (SettingsGameScreen.Current != null)
            ScreenManager.RemoveScreen(SettingsGameScreen.Current);
    }

    // Run lifecycle delegates
    public static void RunLaunchPostfix(RunState __result)
    {
        if (RunScreen.Current == null)
            ScreenManager.PushScreen(new RunScreen(__result));
    }

    public static void RunEndedPostfix()
    {
        CombatEventManager.CleanUp();
        if (RunScreen.Current != null)
            ScreenManager.RemoveScreen(RunScreen.Current);
    }
}
