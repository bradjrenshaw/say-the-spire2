using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using Sts2AccessibilityMod.UI;

namespace Sts2AccessibilityMod.Hooks;

public static class ScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        var updateMethod = AccessTools.Method(typeof(ActiveScreenContext), "Update");
        if (updateMethod == null)
        {
            Log.Error("[AccessibilityMod] Could not find ActiveScreenContext.Update()!");
            return;
        }

        var postfix = new HarmonyMethod(typeof(ScreenHooks).GetMethod(nameof(UpdatePostfix), BindingFlags.Static | BindingFlags.Public));
        harmony.Patch(updateMethod, postfix: postfix);
        Log.Info("[AccessibilityMod] Screen hooks patched successfully.");
    }

    public static void UpdatePostfix()
    {
        GameScreenManager.OnScreenChanged();
    }
}
