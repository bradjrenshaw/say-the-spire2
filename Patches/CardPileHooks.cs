using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class CardPileHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NCardPileScreen), "ShowScreen",
            nameof(CardPileShowPostfix), "CardPile ShowScreen");
        PatchIfFound(harmony, typeof(NCardPileScreen), "AfterCapstoneClosed",
            nameof(CardPileClosedPostfix), "CardPile AfterCapstoneClosed");
        PatchIfFound(harmony, typeof(NDeckViewScreen), "ShowScreen",
            nameof(DeckViewShowPostfix), "DeckView ShowScreen");
        PatchIfFound(harmony, typeof(NDeckViewScreen), "AfterCapstoneClosed",
            nameof(DeckViewClosedPostfix), "DeckView AfterCapstoneClosed");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(CardPileHooks), handlerName, label, isPrefix);
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
