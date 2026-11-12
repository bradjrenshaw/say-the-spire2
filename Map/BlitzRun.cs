using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace SayTheSpire2.Map;

/// <summary>
/// Whether the current run is a Blitz run (November beta). Blitz has no map
/// screen: each act is one fixed line of rooms, shown as a strip of points in
/// the top bar, and Proceed moves straight to the next room.
///
/// Detected by the run's map type rather than the GameMode enum: the enum's
/// members differ between branches (stable has no Blitz, and its value 3 is
/// a different mode than the beta's), while BlitzActMap simply doesn't exist
/// on stable.
/// </summary>
public static class BlitzRun
{
    private static readonly Type? BlitzMapType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Map.BlitzActMap");

    public static bool IsActive =>
        BlitzMapType != null && IsBlitz(RunManager.Instance.DebugOnlyGetState());

    public static bool IsBlitz(IRunState? runState) =>
        BlitzMapType != null && runState != null && BlitzMapType.IsInstanceOfType(runState.Map);
}
