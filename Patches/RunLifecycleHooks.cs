using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Events;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class RunLifecycleHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(RunManager), "Launch",
            typeof(RunLifecycleHooks), nameof(RunLaunchPostfix), "Run Launch");
        HarmonyHelper.PatchIfFound(harmony, typeof(RunManager), "OnEnded",
            typeof(RunLifecycleHooks), nameof(RunEndedPostfix), "Run OnEnded");
        HarmonyHelper.PatchIfFound(harmony, typeof(RunManager), "CleanUp",
            typeof(RunLifecycleHooks), nameof(RunCleanUpPrefix), "Run CleanUp", isPrefix: true);
    }

    public static void RunLaunchPostfix(RunState __result)
    {
        if (RunScreen.Current == null)
            ScreenManager.PushScreen(new RunScreen());
    }

    public static void RunEndedPostfix()
    {
        CombatEventManager.CleanUp();
        if (RunScreen.Current != null)
            ScreenManager.RemoveScreen(RunScreen.Current);
    }

    /// <summary>
    /// RunManager.CleanUp is called by save-and-quit (ReturnToMainMenu) and does
    /// NOT call OnEnded, so we need a separate hook to pop our screens.
    /// Using a prefix so CombatManager.Instance is still alive for unsubscription.
    /// </summary>
    public static void RunCleanUpPrefix()
    {
        CombatEventManager.CleanUp();
        if (RunScreen.Current != null)
            ScreenManager.RemoveScreen(RunScreen.Current);
    }
}
