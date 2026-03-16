using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class GameOverHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NGameOverScreen), "InitializeBannerAndQuote",
            nameof(GameOverBannerPostfix), "GameOver banner");
        PatchIfFound(harmony, typeof(NGameOverScreen), "AddBadge",
            nameof(AddBadgePostfix), "GameOver AddBadge");
        PatchIfFound(harmony, typeof(NGameOverScreen), "AnimateScoreBar",
            nameof(AnimateScoreBarPrefix), "GameOver AnimateScoreBar", isPrefix: true);
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(GameOverHooks), handlerName, label, isPrefix);
    }

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
}
