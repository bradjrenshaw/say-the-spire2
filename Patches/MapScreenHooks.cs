using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;
using MapScreen = SayTheSpire2.Map.MapScreen;

namespace SayTheSpire2.Patches;

public static class MapScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        PatchIfFound(harmony, typeof(NMapScreen), "Open",
            nameof(MapScreenOpenPostfix), "MapScreen Open");
        PatchIfFound(harmony, typeof(NMapScreen), "Close",
            nameof(MapScreenClosePostfix), "MapScreen Close");
        PatchIfFound(harmony, typeof(NActBanner), "Create",
            nameof(ActBannerCreatePostfix), "ActBanner Create");
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        HarmonyHelper.PatchIfFound(harmony, type, methodName, typeof(MapScreenHooks), handlerName, label, isPrefix);
    }

    public static void MapScreenOpenPostfix()
    {
        if (MapScreen.Current != null) return;

        MapPoint? currentPoint = null;
        try
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            currentPoint = runState?.CurrentMapPoint;
        }
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Map current point access failed: {e.Message}"); }

        var screen = new MapScreen(currentPoint);
        ScreenManager.PushScreen(screen);
    }

    public static void MapScreenClosePostfix()
    {
        if (MapScreen.Current != null)
            ScreenManager.RemoveScreen(MapScreen.Current);
    }

    public static void ActBannerCreatePostfix(ActModel act, int actIndex, NActBanner? __result)
    {
        if (__result == null) return;
        try
        {
            var actNumber = new LocString("gameplay_ui", "ACT_NUMBER");
            actNumber.Add("actNumber", actIndex + 1);
            var numberText = actNumber.GetFormattedText();
            var nameText = act.Title.GetFormattedText();
            SpeechManager.Output(Message.Raw($"{numberText}, {nameText}"));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Act banner speech error: {e.Message}");
        }
    }
}
