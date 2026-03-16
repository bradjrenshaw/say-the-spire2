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
        PatchIfFound(harmony, typeof(NTimelineScreen), "EnableInput",
            nameof(TimelineEnableInputPostfix), "Timeline EnableInput");

        PatchIfFound(harmony, typeof(NUnlockScreen), "Open",
            nameof(UnlockScreenOpenPostfix), "Unlock screen Open");

        PatchIfFound(harmony, typeof(NEpochInspectScreen), "Open",
            nameof(EpochInspectOpenPostfix), "Epoch inspect Open");
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "OpenViaPaginator",
            nameof(EpochPaginatePostfix), "Epoch paginate");
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "Close",
            nameof(EpochInspectClosedPostfix), "Epoch inspect Close");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(TimelineHooks), handlerName, label, isPrefix);
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
