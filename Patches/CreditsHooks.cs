using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Credits;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

/// <summary>
/// Pushes a CreditsGameScreen wrapper when NCreditsScreen opens. The credits
/// screen has zero focusable inner controls and 100% hardcoded structure on
/// the game side — there's no data model to consume — so the wrapper observes
/// the live scene each frame and announces labels as they scroll into view.
/// </summary>
public static class CreditsHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NCreditsScreen), "_Ready",
            typeof(CreditsHooks), nameof(ReadyPostfix), "NCreditsScreen._Ready");
    }

    public static void ReadyPostfix(NCreditsScreen __instance)
    {
        try
        {
            if (CreditsGameScreen.Current != null) return;
            ScreenManager.PushScreen(new CreditsGameScreen(__instance));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] CreditsHooks Ready postfix error: {e.Message}");
        }
    }
}
