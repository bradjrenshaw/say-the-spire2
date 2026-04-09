using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class ScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        var updateMethod = AccessTools.Method(typeof(ActiveScreenContext), "Update");
        if (updateMethod != null)
        {
            harmony.Patch(updateMethod,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(UpdatePostfix)));
            Log.Info("[AccessibilityMod] Screen hooks patched successfully.");
        }
    }

    public static void UpdatePostfix() => ScreenManager.OnGameScreenChanged();
}
