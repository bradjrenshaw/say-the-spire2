using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class HandSelectHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NPlayerHand), "SelectCards",
            typeof(HandSelectHooks), nameof(SelectCardsPostfix), "Hand SelectCards");
        HarmonyHelper.PatchIfFound(harmony, typeof(NPlayerHand), "AfterCardsSelected",
            typeof(HandSelectHooks), nameof(HandSelectClosedPostfix), "Hand AfterCardsSelected");
    }

    public static void SelectCardsPostfix(NPlayerHand __instance, CardSelectorPrefs prefs)
    {
        if (HandSelectGameScreen.Current != null) return;

        string label = "Card Selection";
        try
        {
            var text = prefs.Prompt.GetFormattedText();
            if (!string.IsNullOrEmpty(text))
                label = text;
        }
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Hand select prompt access failed: {e.Message}"); }

        var handScreen = new HandSelectGameScreen(__instance, label);
        if (CombatScreen.Current != null)
            CombatScreen.Current.PushChild(handScreen);
        else
            ScreenManager.PushScreen(handScreen);
        SpeechManager.Output(Message.Raw(label));
    }

    public static void HandSelectClosedPostfix()
    {
        if (HandSelectGameScreen.Current != null)
            ScreenManager.RemoveFromTree(HandSelectGameScreen.Current);
    }
}
