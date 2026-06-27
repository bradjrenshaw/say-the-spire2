using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.Patches;

/// <summary>
/// Fixes "down from the top bar doesn't return to the active screen" on the
/// beta branch.
///
/// Beta routes the relic row's bottom focus-neighbor through a shared
/// <c>%ActiveScreenProxy</c> node whose FocusEntered is meant to forward focus
/// into the current screen — but that hop never lands for keyboard/controller
/// focus, so jumping to the top bar (e.g. via the top-panel hotkey) during
/// combat, a card selection (Choices Paradox, etc.), or the map strands focus
/// with no way back down.
///
/// The stable branch instead points the relic row directly at the active
/// screen's <see cref="IScreenContext.FocusedControlFromTopBar"/>, recomputed on
/// every screen change (<c>ActiveScreenContext.Updated</c>), and that works.
/// Replicate it on beta: subscribe to the same event and re-point the relic row
/// ourselves, bypassing the proxy.
///
/// Gated to beta (the property only exists there; stable already does this
/// natively) and singleplayer (multiplayer wires the relic row through the
/// player-state column, which must stay intact).
/// </summary>
public static class TopBarReturnHooks
{
    private static readonly PropertyInfo? ActiveScreenProxyProp =
        AccessTools.Property(typeof(NTopBar), "ActiveScreenProxy");

    private static ulong _lastTargetId;
    private static int _lastRelicCount = -1;

    public static void Initialize()
    {
        // Stable wires the relic row to FocusedControlFromTopBar natively, so
        // this is only needed where ActiveScreenProxy exists (beta).
        if (ActiveScreenProxyProp == null)
            return;

        ActiveScreenContext.Instance.Updated += OnScreenUpdated;
        Log.Info("[AccessibilityMod] Top-bar return fix active (beta relic-row rewire).");
    }

    private static void OnScreenUpdated()
    {
        try
        {
            if (!MultiplayerHelper.IsSingleplayerOrFakeMultiplayer())
                return;

            var relics = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes;
            if (relics == null || relics.Count == 0)
            {
                _lastTargetId = 0;
                _lastRelicCount = -1;
                return;
            }

            var target = ActiveScreenContext.Instance.GetCurrentScreen()?.FocusedControlFromTopBar;
            if (target == null || !GodotObject.IsInstanceValid(target))
                return;

            // Re-point when the destination changes (screen switch) or when the
            // relic set changes (the game resets the row to the proxy on relic
            // gain, so we re-apply over it).
            if (target.GetInstanceId() == _lastTargetId && relics.Count == _lastRelicCount)
                return;
            _lastTargetId = target.GetInstanceId();
            _lastRelicCount = relics.Count;

            var path = target.GetPath();
            foreach (var relic in relics)
                relic.FocusNeighborBottom = path;
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Top-bar return rewire failed: {e.Message}");
        }
    }
}
