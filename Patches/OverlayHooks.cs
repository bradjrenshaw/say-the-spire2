using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class OverlayHooks
{
    public static void Initialize(Harmony harmony)
    {
        HarmonyHelper.PatchIfFound(harmony, typeof(NOverlayStack), "Push",
            typeof(OverlayHooks), nameof(OverlayPushPostfix), "Overlay Push");
        HarmonyHelper.PatchIfFound(harmony, typeof(NOverlayStack), "Remove",
            typeof(OverlayHooks), nameof(OverlayRemovePostfix), "Overlay Remove");

        // Targeting focus (foul potion in shop)
        var startTargeting = AccessTools.Method(typeof(NTargetManager), "StartTargeting",
            new[] { typeof(TargetType), typeof(Vector2), typeof(TargetMode), typeof(System.Func<bool>), typeof(System.Func<Node, bool>) });
        if (startTargeting != null)
        {
            harmony.Patch(startTargeting,
                postfix: new HarmonyMethod(typeof(OverlayHooks), nameof(StartTargetingPostfix)));
            Log.Info("[AccessibilityMod] StartTargeting hook patched.");
        }

        // Multiplayer expanded player state (capstone screen)
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerPlayerExpandedState), "AfterCapstoneOpened",
            typeof(OverlayHooks), nameof(PlayerExpandedOpenedPostfix), "PlayerExpanded AfterCapstoneOpened");
        HarmonyHelper.PatchIfFound(harmony, typeof(NMultiplayerPlayerExpandedState), "AfterCapstoneClosed",
            typeof(OverlayHooks), nameof(PlayerExpandedClosedPostfix), "PlayerExpanded AfterCapstoneClosed");

        // Bundle preview focus setup
        HarmonyHelper.PatchIfFound(harmony, typeof(NChooseABundleSelectionScreen), "OnBundleClicked",
            typeof(OverlayHooks), nameof(BundlePreviewPostfix), "Bundle preview focus");
        HarmonyHelper.PatchIfFound(harmony, typeof(NChooseABundleSelectionScreen), "CancelSelection",
            typeof(OverlayHooks), nameof(BundleCancelPostfix), "Bundle cancel focus");
    }

    public static void OverlayPushPostfix(IOverlayScreen screen)
    {
        if (screen is NCardGridSelectionScreen gridScreen
            && CardGridSelectionGameScreen.Current == null)
        {
            if (CombatScreen.Current != null)
                CombatScreen.Current.PushChild(new CardGridSelectionGameScreen(gridScreen));
            else
                ScreenManager.PushScreen(new CardGridSelectionGameScreen(gridScreen));
        }
        else if (screen is NChooseACardSelectionScreen chooseScreen
            && ChooseACardGameScreen.Current == null)
        {
            if (CombatScreen.Current != null)
                CombatScreen.Current.PushChild(new ChooseACardGameScreen(chooseScreen));
            else
                ScreenManager.PushScreen(new ChooseACardGameScreen(chooseScreen));
        }
        else if (screen is NCardRewardSelectionScreen rewardScreen
            && CardRewardGameScreen.Current == null)
        {
            if (CombatScreen.Current != null)
                CombatScreen.Current.PushChild(new CardRewardGameScreen(rewardScreen));
            else
                ScreenManager.PushScreen(new CardRewardGameScreen(rewardScreen));
        }
        else if (screen is NCrystalSphereScreen crystalScreen
            && CrystalSphereGameScreen.Current == null)
        {
            ScreenManager.PushScreen(new CrystalSphereGameScreen(crystalScreen));
        }
        else if (screen is NRewardsScreen rewardsScreen
            && RewardsGameScreen.Current == null)
        {
            ScreenManager.PushScreen(new RewardsGameScreen(rewardsScreen));
        }
    }

    public static void OverlayRemovePostfix(IOverlayScreen screen)
    {
        if (screen is NCardGridSelectionScreen
            && CardGridSelectionGameScreen.Current != null)
        {
            ScreenManager.RemoveFromTree(CardGridSelectionGameScreen.Current);
        }
        else if (screen is NChooseACardSelectionScreen
            && ChooseACardGameScreen.Current != null)
        {
            ScreenManager.RemoveFromTree(ChooseACardGameScreen.Current);
        }
        else if (screen is NCardRewardSelectionScreen
            && CardRewardGameScreen.Current != null)
        {
            ScreenManager.RemoveFromTree(CardRewardGameScreen.Current);
        }
        else if (screen is NCrystalSphereScreen
            && CrystalSphereGameScreen.Current != null)
        {
            ScreenManager.RemoveScreen(CrystalSphereGameScreen.Current);
        }
        else if (screen is NRewardsScreen
            && RewardsGameScreen.Current != null)
        {
            ScreenManager.RemoveScreen(RewardsGameScreen.Current);
        }
        else if (screen is NGameOverScreen
            && GameOverScreen.Current != null)
        {
            ScreenManager.RemoveScreen(GameOverScreen.Current);
        }
    }

    public static void StartTargetingPostfix(TargetType validTargetsType)
    {
        try
        {
            if (validTargetsType != TargetType.TargetedNoCreature) return;
            if (!RunManager.Instance.IsInProgress) return;

            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return;
            var room = runState.CurrentRoom;
            if (room?.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Shop) return;

            var merchantRoom = NMerchantRoom.Instance;
            if (merchantRoom == null) return;

            var merchantButton = merchantRoom.MerchantButton;
            if (merchantButton == null) return;

            var control = (Control)merchantButton;
            control.FocusMode = Control.FocusModeEnum.All;
            control.GrabFocus();
            NTargetManager.Instance.OnNodeHovered(merchantButton);
            SpeechManager.Output(Message.Localized("ui", "SPEECH.MERCHANT"));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] StartTargeting focus error: {e.Message}");
        }
    }

    public static void BundlePreviewPostfix(NChooseABundleSelectionScreen __instance, NCardBundle bundleNode)
    {
        try
        {
            var cardsField = AccessTools.Field(typeof(NChooseABundleSelectionScreen), "_bundlePreviewCards");
            var previewCards = cardsField?.GetValue(__instance) as Control;
            if (previewCards == null || previewCards.GetChildCount() == 0) return;

            var bundleRowField = AccessTools.Field(typeof(NChooseABundleSelectionScreen), "_bundleRow");
            var bundleRow = bundleRowField?.GetValue(__instance) as Control;
            if (bundleRow != null)
            {
                for (int i = 0; i < bundleRow.GetChildCount(); i++)
                {
                    var bundle = bundleRow.GetChild(i) as NCardBundle;
                    if (bundle?.Hitbox is Control hitbox)
                        hitbox.FocusMode = Control.FocusModeEnum.None;
                }
            }

            var bundleCards = new System.Collections.Generic.HashSet<CardModel>();
            foreach (var card in bundleNode.Bundle)
                bundleCards.Add(card);

            var holders = new System.Collections.Generic.List<Control>();
            for (int i = 0; i < previewCards.GetChildCount(); i++)
            {
                var holder = previewCards.GetChild(i) as NCardHolder;
                if (holder?.CardModel != null && bundleCards.Contains(holder.CardModel))
                {
                    holder.FocusMode = Control.FocusModeEnum.All;
                    holders.Add(holder);
                }
                else if (holder != null)
                {
                    holder.FocusMode = Control.FocusModeEnum.None;
                    holder.FocusNeighborLeft = null;
                    holder.FocusNeighborRight = null;
                    holder.FocusNeighborTop = null;
                    holder.FocusNeighborBottom = null;
                }
            }

            if (holders.Count == 0) return;

            for (int i = 0; i < holders.Count; i++)
            {
                var left = i > 0 ? holders[i - 1] : holders[holders.Count - 1];
                var right = i < holders.Count - 1 ? holders[i + 1] : holders[0];
                holders[i].FocusNeighborLeft = left.GetPath();
                holders[i].FocusNeighborRight = right.GetPath();
                holders[i].FocusNeighborTop = holders[i].GetPath();
                holders[i].FocusNeighborBottom = holders[i].GetPath();
            }

            holders[0].GrabFocus();
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Bundle preview focus error: {e.Message}");
        }
    }

    public static void BundleCancelPostfix(NChooseABundleSelectionScreen __instance)
    {
        try
        {
            var cardsField = AccessTools.Field(typeof(NChooseABundleSelectionScreen), "_bundlePreviewCards");
            var previewCards = cardsField?.GetValue(__instance) as Control;
            if (previewCards != null)
            {
                for (int i = 0; i < previewCards.GetChildCount(); i++)
                {
                    var holder = previewCards.GetChild(i) as Control;
                    if (holder != null)
                    {
                        holder.FocusMode = Control.FocusModeEnum.None;
                        holder.FocusNeighborLeft = null;
                        holder.FocusNeighborRight = null;
                        holder.FocusNeighborTop = null;
                        holder.FocusNeighborBottom = null;
                    }
                }
            }

            var bundleRowField = AccessTools.Field(typeof(NChooseABundleSelectionScreen), "_bundleRow");
            var bundleRow = bundleRowField?.GetValue(__instance) as Control;
            if (bundleRow == null) return;

            for (int i = 0; i < bundleRow.GetChildCount(); i++)
            {
                var bundle = bundleRow.GetChild(i) as NCardBundle;
                if (bundle?.Hitbox is Control hitbox)
                    hitbox.FocusMode = Control.FocusModeEnum.All;
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Bundle cancel focus error: {e.Message}");
        }
    }

    public static void PlayerExpandedOpenedPostfix(NMultiplayerPlayerExpandedState __instance)
    {
        if (PlayerExpandedStateScreen.Current == null)
            ScreenManager.PushScreen(new PlayerExpandedStateScreen(__instance));
    }

    public static void PlayerExpandedClosedPostfix()
    {
        if (PlayerExpandedStateScreen.Current != null)
            ScreenManager.RemoveScreen(PlayerExpandedStateScreen.Current);
    }
}
