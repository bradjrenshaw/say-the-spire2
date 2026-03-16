using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
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
        catch { }

        var screen = new MapScreen(currentPoint);
        ScreenManager.PushScreen(screen);
    }

    public static void MapScreenClosePostfix()
    {
        if (MapScreen.Current != null)
            ScreenManager.RemoveScreen(MapScreen.Current);
    }
}
