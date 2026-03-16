using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace SayTheSpire2.Patches;

public static class HarmonyHelper
{
    /// <summary>
    /// Patch a method if it exists, logging success or failure.
    /// </summary>
    public static void PatchIfFound(Harmony harmony, Type targetType, string methodName,
        Type handlerType, string handlerMethodName, string label, bool isPrefix = false)
    {
        var method = AccessTools.Method(targetType, methodName);
        if (method == null)
        {
            Log.Error($"[AccessibilityMod] Could not find {targetType.Name}.{methodName} for {label}!");
            return;
        }

        var handler = new HarmonyMethod(handlerType, handlerMethodName);
        if (isPrefix)
            harmony.Patch(method, prefix: handler);
        else
            harmony.Patch(method, postfix: handler);
        Log.Info($"[AccessibilityMod] {label} hook patched.");
    }
}
