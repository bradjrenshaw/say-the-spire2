using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class RestSiteHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NRestSiteRoom), "_Ready",
            nameof(RestSiteReadyPostfix), "RestSite _Ready");
        PatchIfFound(harmony, typeof(NRestSiteRoom), "_ExitTree",
            nameof(RestSiteExitPostfix), "RestSite _ExitTree");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(RestSiteHooks), handlerName, label, isPrefix);
    }

    public static void RestSiteReadyPostfix(NRestSiteRoom __instance)
    {
        if (RestSiteGameScreen.Current == null)
            ScreenManager.PushScreen(new RestSiteGameScreen(__instance));
    }

    public static void RestSiteExitPostfix()
    {
        if (RestSiteGameScreen.Current != null)
            ScreenManager.RemoveScreen(RestSiteGameScreen.Current);
    }
}
