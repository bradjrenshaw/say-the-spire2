using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SayTheSpire2.Events;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

/// <summary>
/// Harmony registration for combat navigation fixes.
/// All behavior lives in CombatScreen.
/// </summary>
public static class CombatNavigationHooks
{
    public static void Initialize(Harmony harmony)
    {
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

        var takeTurn = AccessTools.Method(typeof(Creature), "TakeTurn");
        if (takeTurn != null)
        {
            harmony.Patch(takeTurn,
                prefix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(TakeTurnPrefix)));
            Log.Info("[AccessibilityMod] Creature.TakeTurn hook patched.");
        }

        var shuffle = AccessTools.Method(typeof(CardPileCmd), "Shuffle");
        if (shuffle != null)
        {
            harmony.Patch(shuffle,
                prefix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(ShufflePrefix)),
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(ShufflePostfix)));
            Log.Info("[AccessibilityMod] CardPileCmd.Shuffle hook patched.");
        }

        // Patch NCreature.UpdateNavigation to fix player's upward nav after orb setup
        var updateCreatureNav = AccessTools.Method(typeof(NCreature), "UpdateNavigation");
        if (updateCreatureNav != null)
        {
            harmony.Patch(updateCreatureNav,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(CreatureUpdateNavigationPostfix)));
            Log.Info("[AccessibilityMod] NCreature.UpdateNavigation hook patched.");
        }

        // When a creature becomes interactable, ensure its hitbox is focusable.
        // New creatures spawned mid-combat (e.g. boss resummon) may not have
        // FocusMode=All since EnableControllerNavigation only runs at combat start.
        var toggleInteractable = AccessTools.Method(typeof(NCreature), "ToggleIsInteractable");
        if (toggleInteractable != null)
        {
            harmony.Patch(toggleInteractable,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(ToggleIsInteractablePostfix)));
            Log.Info("[AccessibilityMod] NCreature.ToggleIsInteractable hook patched.");
        }

        var refreshLayout = AccessTools.Method(typeof(NPlayerHand), "RefreshLayout");
        if (refreshLayout != null)
        {
            harmony.Patch(refreshLayout,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(RefreshLayoutPostfix)));
            Log.Info("[AccessibilityMod] Hand card navigation hook patched.");
        }

        // Fix navigation after card selection (e.g. Survivor discard choice).
        // AfterCardsSelected calls RestrictControllerNavigation but never re-enables it.
        var afterCardsSelected = AccessTools.Method(typeof(NPlayerHand), "AfterCardsSelected");
        if (afterCardsSelected != null)
        {
            harmony.Patch(afterCardsSelected,
                postfix: new HarmonyMethod(typeof(CombatNavigationHooks),
                    nameof(AfterCardsSelectedPostfix)));
            Log.Info("[AccessibilityMod] AfterCardsSelected hook patched.");
        }
    }

    public static bool UpdateCreatureNavigationPrefix()
    {
        try
        {
            if (NTargetManager.Instance?.IsInSelection == true)
                return false;
        }
        catch { }
        return true;
    }

    public static void UpdateCreatureNavigationPostfix(NCombatRoom __instance)
    {
        // Run directly without depending on CombatScreen.Current, since
        // UpdateCreatureNavigation fires during AddCreature before CombatSetUp.
        try
        {
            SetCreatureFocusToRelics(__instance);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Creature navigation postfix failed: {e.Message}");
        }
    }

    public static void StartTargetingPostfix()
        => CombatScreen.Current?.OnTargetingStarted();

    public static void FinishTargetingPostfix()
    {
        CombatScreen.Current?.OnTargetingFinished();
        // Also fix navigation directly in case CombatScreen.Current is null
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom != null)
                SetCreatureFocusToRelics(combatRoom);
        }
        catch { }
    }

    public static void CreatureUpdateNavigationPostfix(NCreature __instance)
    {
        try
        {
            if (!__instance.Entity.IsPlayer) return;

            var firstRelic = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes?.FirstOrDefault();
            if (firstRelic == null || !GodotObject.IsInstanceValid(firstRelic)) return;

            var relicPath = firstRelic.GetPath();

            // After UpdateNavigation, hitbox points to OrbManager.DefaultFocusOwner.
            // Set that control's FocusNeighborTop to relics.
            if (__instance.OrbManager != null)
            {
                var orbFocus = __instance.OrbManager.DefaultFocusOwner;
                if (orbFocus != null)
                    orbFocus.FocusNeighborTop = relicPath;
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] CreatureUpdateNavigation postfix failed: {e.Message}");
        }
    }

    public static void ToggleIsInteractablePostfix(NCreature __instance, bool on)
    {
        try
        {
            if (on && __instance.Hitbox != null)
                __instance.Hitbox.FocusMode = Control.FocusModeEnum.All;
        }
        catch { }
    }

    public static void RefreshLayoutPostfix(NPlayerHand __instance)
        => CombatScreen.Current?.OnHandLayoutRefreshed(__instance);

    public static void AfterCardsSelectedPostfix()
    {
        // AfterCardsSelected never calls EnableControllerNavigation, leaving
        // creature hitboxes with FocusMode=None. Re-enable them so hand→creature
        // navigation works after card selection (e.g. Survivor discard).
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;
            combatRoom.EnableControllerNavigation();
            SetCreatureFocusToRelics(combatRoom);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] AfterCardsSelected postfix failed: {e.Message}");
        }
    }

    public static void ShufflePrefix()
        => CombatScreen.Current?.OnShuffleStarting();

    public static void ShufflePostfix(Task __result)
        => CombatScreen.Current?.OnShuffleStarted(__result);

    private static void SetCreatureFocusToRelics(NCombatRoom combatRoom)
    {
        var firstRelic = NRun.Instance?.GlobalUi?.RelicInventory?.RelicNodes?.FirstOrDefault();
        if (firstRelic == null || !GodotObject.IsInstanceValid(firstRelic)) return;

        var relicPath = firstRelic.GetPath();

        foreach (var creature in combatRoom.CreatureNodes)
        {
            if (creature == null) continue;
            var hitbox = creature.Hitbox;
            if (hitbox == null) continue;

            if (creature.Entity.IsPlayer && creature.OrbManager != null)
            {
                // Player hitbox points up to orbs (set by NCreature.UpdateNavigation).
                // Set the orb manager's top focus to relics so up from orbs reaches relics.
                var orbFocus = creature.OrbManager.DefaultFocusOwner;
                if (orbFocus != null)
                    orbFocus.FocusNeighborTop = relicPath;
            }
            else
            {
                hitbox.FocusNeighborTop = relicPath;
            }
        }
    }

    public static void TakeTurnPrefix(Creature __instance)
    {
        try
        {
            if (__instance.IsMonster && __instance.Monster != null && !__instance.Monster.SpawnedThisTurn)
                EventDispatcher.Enqueue(new EnemyMoveEvent(__instance));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] TakeTurn prefix error: {e.Message}");
        }
    }
}
