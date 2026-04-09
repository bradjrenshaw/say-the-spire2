using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class CardPileHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NCardPileScreen), "ShowScreen",
            typeof(CardPileHooks), nameof(CardPileShowPostfix), "CardPile ShowScreen");
        HarmonyHelper.PatchIfFound(harmony, typeof(NCardPileScreen), "AfterCapstoneClosed",
            typeof(CardPileHooks), nameof(CardPileClosedPostfix), "CardPile AfterCapstoneClosed");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDeckViewScreen), "ShowScreen",
            typeof(CardPileHooks), nameof(DeckViewShowPostfix), "DeckView ShowScreen");
        HarmonyHelper.PatchIfFound(harmony, typeof(NDeckViewScreen), "AfterCapstoneClosed",
            typeof(CardPileHooks), nameof(DeckViewClosedPostfix), "DeckView AfterCapstoneClosed");
    }

    public static void CardPileShowPostfix(NCardPileScreen __result)
    {
        if (CardPileGameScreen.Current == null)
            ScreenManager.PushScreen(new CardPileGameScreen(__result));
    }

    public static void CardPileClosedPostfix(NCardPileScreen __instance)
    {
        if (CardPileGameScreen.Current != null)
            ScreenManager.RemoveScreen(CardPileGameScreen.Current);
    }

    public static void DeckViewShowPostfix(NDeckViewScreen __result)
    {
        if (__result != null && CardPileGameScreen.Current == null)
            ScreenManager.PushScreen(new CardPileGameScreen(__result));
    }

    public static void DeckViewClosedPostfix(NDeckViewScreen __instance)
    {
        if (CardPileGameScreen.Current != null)
            ScreenManager.RemoveScreen(CardPileGameScreen.Current);
    }
}
