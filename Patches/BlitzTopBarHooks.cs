using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.Patches;

/// <summary>
/// In a Blitz run, makes Up from anywhere on the top bar jump to the first
/// point of the Blitz map strip, so the act's room line can be checked
/// quickly without walking left/right through HP, potions and the rest.
///
/// The game wires every top-bar element's upward neighbour to itself (Up
/// does nothing there), so this takes over an otherwise dead direction. The
/// strip's own points keep that self-loop: Up from the strip stays put.
///
/// Applied after each <c>NTopBar.UpdateNavigation</c>, which is where the
/// game (re)writes those neighbours. The game doesn't run that when the
/// strip itself is rebuilt — every act change frees the old points and
/// creates new ones — which would leave Up (and the game's own left/right
/// wiring into the strip) aimed at freed nodes until some later screen
/// change. So a rebuild of the strip triggers the navigation update too.
/// Beta-only: skipped where the top bar has no Blitz map.
/// </summary>
public static class BlitzTopBarHooks
{
    // Beta-only; null on stable.
    private static readonly PropertyInfo? BlitzMapProp =
        AccessTools.Property(typeof(NTopBar), "BlitzMap");

    private static PropertyInfo? _mapPointsProp;
    // Also beta-only (stable's top bar has no such method), so not called directly.
    private static MethodInfo? _navigableElementsMethod;
    // Not public on stable, so reached the same way.
    private static MethodInfo? _updateNavigationMethod;

    public static void Initialize(Harmony harmony)
    {
        if (BlitzMapProp == null)
            return;

        // Resolved only where the type exists; a rename there should crash.
        _mapPointsProp = AccessTools.Property(BlitzMapProp.PropertyType, "MapPoints")!;
        _navigableElementsMethod = AccessTools.Method(typeof(NTopBar), "GetAllNavigableElements")!;

        _updateNavigationMethod = AccessTools.Method(typeof(NTopBar), "UpdateNavigation")!;

        HarmonyHelper.PatchIfFound(harmony, typeof(NTopBar), "UpdateNavigation",
            typeof(BlitzTopBarHooks), nameof(UpdateNavigationPostfix), "TopBar UpdateNavigation (Blitz map)");
        HarmonyHelper.PatchIfFound(harmony, BlitzMapProp.PropertyType, "SetMap",
            typeof(BlitzTopBarHooks), nameof(SetMapPostfix), "BlitzMap SetMap");
    }

    // The strip was just rebuilt (new act, or the run's first map). Deferred:
    // the game may add the new points to the tree with a deferred call, and
    // they have to be in it to count as navigable.
    public static void SetMapPostfix()
    {
        Callable.From(RefreshTopBarNavigation).CallDeferred();
    }

    private static void RefreshTopBarNavigation()
    {
        try
        {
            var topBar = NRun.Instance?.GlobalUi?.TopBar;
            if (topBar != null && GodotObject.IsInstanceValid(topBar))
                _updateNavigationMethod?.Invoke(topBar, null);
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Blitz top-bar navigation refresh failed: {e.Message}");
        }
    }

    public static void UpdateNavigationPostfix(NTopBar __instance)
    {
        try
        {
            // Empty outside Blitz runs, where the strip is never populated.
            if (_mapPointsProp?.GetValue(BlitzMapProp?.GetValue(__instance)) is not IEnumerable points)
                return;
            var first = points.OfType<Control>().FirstOrDefault(p => p.IsVisibleInTree());
            if (first == null)
                return;

            var path = first.GetPath();
            if (_navigableElementsMethod?.Invoke(__instance, null) is not IEnumerable elements)
                return;
            foreach (var control in elements.OfType<Control>())
            {
                if (!ProxyBlitzMapPoint.Handles(control))
                    control.FocusNeighborTop = path;
            }
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Blitz top-bar navigation rewire failed: {e.Message}");
        }
    }
}
