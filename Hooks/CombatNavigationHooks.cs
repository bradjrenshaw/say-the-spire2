using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Sts2AccessibilityMod.Hooks;

/// <summary>
/// Fixes combat controller navigation:
/// - Creatures up → relics (instead of self-loop)
/// - During targeting, creatures self-loop to stay in creature row
/// - Hand cards up → first creature
/// </summary>
public static class CombatNavigationHooks
{
    public static void Initialize(Harmony harmony)
    {
        // Patch creature navigation - postfix to override FocusNeighborTop,
        // prefix to block during targeting
        var updateNav = AccessTools.Method(typeof(NCombatRoom), "UpdateCreatureNavigation");
        if (updateNav != null)
        {
            harmony.Patch(updateNav,
                prefix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(UpdateCreatureNavigationPrefix)),
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(UpdateCreatureNavigationPostfix)));
            Log.Info("[AccessibilityMod] Combat creature navigation hook patched.");
        }

        // Patch targeting start/end to toggle creature navigation
        // Patch both StartTargeting overloads
        var startTargetingMethods = typeof(NTargetManager).GetMethods()
            .Where(m => m.Name == "StartTargeting");
        foreach (var method in startTargetingMethods)
        {
            harmony.Patch(method,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(StartTargetingPostfix)));
            Log.Info($"[AccessibilityMod] StartTargeting hook patched ({method.GetParameters().Length} params).");
        }

        var finishTargeting = AccessTools.Method(typeof(NTargetManager), "FinishTargeting");
        if (finishTargeting != null)
        {
            harmony.Patch(finishTargeting,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(FinishTargetingPostfix)));
            Log.Info("[AccessibilityMod] FinishTargeting hook patched.");
        }

        // Patch hand card navigation
        var refreshLayout = AccessTools.Method(typeof(NPlayerHand), "RefreshLayout");
        if (refreshLayout != null)
        {
            harmony.Patch(refreshLayout,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(RefreshLayoutPostfix)));
            Log.Info("[AccessibilityMod] Hand card navigation hook patched.");
        }
    }

    /// <summary>
    /// Block UpdateCreatureNavigation during targeting so our self-loops aren't overwritten.
    /// </summary>
    public static bool UpdateCreatureNavigationPrefix()
    {
        try
        {
            if (NTargetManager.Instance?.IsInSelection == true)
                return false; // skip original
        }
        catch { }
        return true; // run original
    }

    /// <summary>
    /// After creature navigation updates, point all creatures up to relics.
    /// </summary>
    public static void UpdateCreatureNavigationPostfix(NCombatRoom __instance)
    {
        try
        {
            SetCreaturesToRelics(__instance);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Creature navigation postfix failed: {e.Message}");
        }
    }

    /// <summary>
    /// When targeting starts, set creatures to self-loop so focus stays in creature row.
    /// </summary>
    public static void StartTargetingPostfix()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            foreach (var creature in combatRoom.CreatureNodes)
            {
                if (creature == null) continue;
                var hitbox = creature.Hitbox;
                if (hitbox == null) continue;
                hitbox.FocusNeighborTop = hitbox.GetPath();
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] StartTargeting postfix failed: {e.Message}");
        }
    }

    /// <summary>
    /// When targeting ends, restore creatures pointing up to relics.
    /// </summary>
    public static void FinishTargetingPostfix()
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;
            SetCreaturesToRelics(combatRoom);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] FinishTargeting postfix failed: {e.Message}");
        }
    }

    private static void SetCreaturesToRelics(NCombatRoom combatRoom)
    {
        var firstRelic = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes?.FirstOrDefault();
        if (firstRelic == null || !GodotObject.IsInstanceValid(firstRelic)) return;

        var relicPath = firstRelic.GetPath();

        foreach (var creature in combatRoom.CreatureNodes)
        {
            if (creature == null) continue;
            var hitbox = creature.Hitbox;
            if (hitbox == null) continue;
            hitbox.FocusNeighborTop = relicPath;
        }
    }

    public static void RefreshLayoutPostfix(NPlayerHand __instance)
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            var firstCreature = combatRoom.CreatureNodes
                .FirstOrDefault(c => c != null && c.IsInteractable && c.Hitbox != null);
            if (firstCreature == null) return;

            var creaturePath = firstCreature.Hitbox.GetPath();

            foreach (var holder in __instance.ActiveHolders)
            {
                if (holder == null) continue;
                holder.FocusNeighborTop = creaturePath;
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Hand navigation postfix failed: {e.Message}");
        }
    }
}
