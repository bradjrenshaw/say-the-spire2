using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using SayTheSpire2.UI.Screens;
using System.Threading.Tasks;

namespace SayTheSpire2.Patches;

public static class GameOverHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "InitializeBannerAndQuote",
            typeof(GameOverHooks), nameof(GameOverBannerPostfix), "GameOver banner");
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "AnimateRunSummary",
            typeof(GameOverHooks), nameof(AnimateRunSummaryPrefix), "GameOver AnimateRunSummary start", isPrefix: true);
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "AnimateRunSummary",
            typeof(GameOverHooks), nameof(AnimateRunSummaryPostfix), "GameOver AnimateRunSummary settled");
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "_ExitTree",
            typeof(GameOverHooks), nameof(GameOverExitPostfix), "GameOver exit");
    }

    public static void GameOverBannerPostfix(NGameOverScreen __instance)
    {
        if (GameOverScreen.Current == null)
            ScreenManager.PushScreen(new GameOverScreen(__instance));
        GameOverScreen.Current?.OnBannerAndQuote(__instance);
    }

    public static void GameOverExitPostfix()
    {
        if (GameOverScreen.Current != null)
            ScreenManager.RemoveScreen(GameOverScreen.Current);
    }

    public static void AnimateRunSummaryPrefix(NGameOverScreen __instance)
        => GameOverScreen.Current?.OnSummaryAnimationStarted(__instance);

    public static void AnimateRunSummaryPostfix(NGameOverScreen __instance, ref Task __result)
    {
        try
        {
            __result = NotifyWhenSummarySettles(__result, __instance);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] GameOver summary settled hook error: {e.Message}");
        }
    }

    private static async Task NotifyWhenSummarySettles(Task original, NGameOverScreen instance)
    {
        await original;

        try
        {
            GameOverScreen.Current?.OnSummarySettled(instance);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] GameOver summary settled callback error: {e.Message}");
        }
    }
}
