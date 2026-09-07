using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Potions;
using SayTheSpire2.Localization;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

/// <summary>
/// Foul Potion can only be thrown while the merchant's item screen is closed
/// (the game targets the merchant button, which is unreachable while the
/// inventory covers it). The game just disables the popup's Throw button with
/// no explanation, so to a blind player pressing Throw appears to do nothing.
/// We add an invisible focusable "notice" control to the potion popup and move
/// focus to it when the user tries to throw, announcing why the throw is
/// blocked and what to do instead.
/// </summary>
public static class PotionPopupHooks
{
    private static readonly FieldInfo HolderField =
        AccessTools.Field(typeof(NPotionPopup), "_holder")!;
    private static readonly FieldInfo UseButtonField =
        AccessTools.Field(typeof(NPotionPopup), "_useButton")!;
    private static readonly FieldInfo DiscardButtonField =
        AccessTools.Field(typeof(NPotionPopup), "_discardButton")!;

    private static readonly ConditionalWeakTable<NPotionPopup, Control> Notices = new();

    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NPotionPopup), "_Ready",
            typeof(PotionPopupHooks), nameof(ReadyPostfix), "PotionPopup Ready");
        HarmonyHelper.PatchIfFound(harmony, typeof(NPotionPopup), "OnUseButtonPressed",
            typeof(PotionPopupHooks), nameof(UseButtonPrefix), "PotionPopup UseButton",
            isPrefix: true);
    }

    public static void ReadyPostfix(NPotionPopup __instance)
    {
        try
        {
            if (!IsThrowBlockedByShop(__instance))
                return;

            var notice = new Control
            {
                FocusMode = Control.FocusModeEnum.All,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            __instance.AddChild(notice);

            // Only auto-focus reaches the notice; arrows lead back to the
            // popup's real buttons.
            var noticePath = notice.GetPath();
            notice.FocusNeighborLeft = noticePath;
            notice.FocusNeighborRight = noticePath;
            if (UseButtonField.GetValue(__instance) is Control useButton)
                notice.FocusNeighborTop = useButton.GetPath();
            if (DiscardButtonField.GetValue(__instance) is Control discardButton)
                notice.FocusNeighborBottom = discardButton.GetPath();

            var element = new ShopThrowNotice { Control = notice };
            notice.FocusEntered += () => UIManager.SetFocusedControl(notice, element);
            Notices.Add(__instance, notice);

            // The game focuses the Discard button here (Throw is disabled),
            // which is exactly the confusing state — the user opened the
            // potion intending to throw it and hears "Discard" with no
            // explanation. Focus the notice instead so the reason is the
            // first thing read; deferred so it lands after the game's own
            // focus grab in _Ready.
            notice.CallDeferred(Control.MethodName.GrabFocus);
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] PotionPopup ready hook error: {e.Message}");
        }
    }

    public static bool UseButtonPrefix(NPotionPopup __instance)
    {
        try
        {
            if (!Notices.TryGetValue(__instance, out var notice)
                || !GodotObject.IsInstanceValid(notice))
                return true;
            // Re-check live: if the shop menu closed while the popup stayed
            // open, the throw is legitimate — let the game handle it.
            if (!IsThrowBlockedByShop(__instance))
                return true;

            notice.GrabFocus();
            return false;
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] PotionPopup use hook error: {e.Message}");
            return true;
        }
    }

    private static bool IsThrowBlockedByShop(NPotionPopup popup)
    {
        if (MerchantGameScreen.Current == null)
            return false;
        if (CombatManager.Instance.IsInProgress)
            return false;
        var potion = (HolderField.GetValue(popup) as NPotionHolder)?.Potion?.Model;
        return potion != null && !potion.PassesCustomUsabilityCheck;
    }

    private sealed class ShopThrowNotice : UIElement
    {
        private static Message Text => Message.Localized("ui", "POTION.THROW_BLOCKED_BY_SHOP");

        public override IEnumerable<Announcement> GetFocusAnnouncements()
        {
            yield return new LabelAnnouncement(Text);
        }

        public override Message? GetLabel() => Text;
    }
}
