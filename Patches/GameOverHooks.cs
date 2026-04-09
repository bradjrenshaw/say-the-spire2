using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class GameOverHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "InitializeBannerAndQuote",
            typeof(GameOverHooks), nameof(GameOverBannerPostfix), "GameOver banner");
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "AddBadge",
            typeof(GameOverHooks), nameof(AddBadgePostfix), "GameOver AddBadge");
        HarmonyHelper.PatchIfFound(harmony, typeof(NGameOverScreen), "AnimateScoreBar",
            typeof(GameOverHooks), nameof(AnimateScoreBarPrefix), "GameOver AnimateScoreBar", isPrefix: true);
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
