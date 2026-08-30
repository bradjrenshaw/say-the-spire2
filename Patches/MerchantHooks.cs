using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class MerchantHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NMerchantInventory), "Open",
            typeof(MerchantHooks), nameof(OpenPostfix), "MerchantInventory Open");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMerchantInventory), "Close",
            typeof(MerchantHooks), nameof(ClosePostfix), "MerchantInventory Close");
    }

    public static void OpenPostfix(NMerchantInventory __instance)
    {
        try
        {
            if (MerchantGameScreen.Current == null)
                ScreenManager.PushScreen(new MerchantGameScreen(__instance));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Merchant open hook error: {e.Message}");
        }
    }

    public static void ClosePostfix()
    {
        try
        {
            if (MerchantGameScreen.Current != null)
                ScreenManager.RemoveScreen(MerchantGameScreen.Current);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Merchant close hook error: {e.Message}");
        }
    }
}
