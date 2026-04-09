using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class TimelineHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NTimelineScreen), "EnableInput",
            typeof(TimelineHooks), nameof(TimelineEnableInputPostfix), "Timeline EnableInput");

        HarmonyHelper.PatchIfFound(harmony, typeof(NUnlockScreen), "Open",
            typeof(TimelineHooks), nameof(UnlockScreenOpenPostfix), "Unlock screen Open");

        HarmonyHelper.PatchIfFound(harmony, typeof(NEpochInspectScreen), "Open",
            typeof(TimelineHooks), nameof(EpochInspectOpenPostfix), "Epoch inspect Open");
        HarmonyHelper.PatchIfFound(harmony, typeof(NEpochInspectScreen), "OpenViaPaginator",
            typeof(TimelineHooks), nameof(EpochPaginatePostfix), "Epoch paginate");
        HarmonyHelper.PatchIfFound(harmony, typeof(NEpochInspectScreen), "Close",
            typeof(TimelineHooks), nameof(EpochInspectClosedPostfix), "Epoch inspect Close");
    }

    public static void TimelineEnableInputPostfix()
        => TimelineGameScreen.Current?.OnEnableInput();

    public static void UnlockScreenOpenPostfix(NUnlockScreen __instance)
        => TimelineGameScreen.Current?.OnUnlockScreenOpen(__instance);

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
}
