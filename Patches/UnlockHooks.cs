using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

/// <summary>
/// Pushes UnlockGameScreen when the beta-introduced NUnlockScreen flow opens
/// (epoch portrait → unlocks list of cards / relics / potions, navigable via
/// controller). Patches the base Open postfix; subclasses call base.Open() so
/// the hook fires for every concrete subclass. Item population happens after
/// base.Open() returns, so the wrapper waits for items via its OnUpdate
/// state-token rebuild rather than registering at Open time.
/// </summary>
public static class UnlockHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NUnlockScreen), "Open",
            typeof(UnlockHooks), nameof(OpenPostfix), "NUnlockScreen.Open");
    }

    public static void OpenPostfix(NUnlockScreen __instance)
    {
        try
        {
            if (UnlockGameScreen.Current != null) return;
            ScreenManager.PushScreen(new UnlockGameScreen(__instance));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] UnlockHooks Open postfix error: {e.Message}");
        }
    }
}
