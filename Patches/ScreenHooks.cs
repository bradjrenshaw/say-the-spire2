using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.Events;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;
using MapScreen = SayTheSpire2.Map.MapScreen;

namespace SayTheSpire2.Patches;

public static class ScreenHooks
{
    public static void Initialize(Harmony harmony)
    {
        // Core screen context change hook
        var updateMethod = AccessTools.Method(typeof(ActiveScreenContext), "Update");
        if (updateMethod != null)
        {
            harmony.Patch(updateMethod,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(UpdatePostfix)));
            Log.Info("[AccessibilityMod] Screen hooks patched successfully.");
        }

        // Game over hooks
        PatchIfFound(harmony, typeof(NGameOverScreen), "InitializeBannerAndQuote",
            nameof(GameOverBannerPostfix), "GameOver banner");
        PatchIfFound(harmony, typeof(NGameOverScreen), "AddBadge",
            nameof(AddBadgePostfix), "GameOver AddBadge");
        PatchIfFound(harmony, typeof(NGameOverScreen), "AnimateScoreBar",
            nameof(AnimateScoreBarPrefix), "GameOver AnimateScoreBar", isPrefix: true);

        // Timeline hooks
        PatchIfFound(harmony, typeof(NTimelineScreen), "EnableInput",
            nameof(TimelineEnableInputPostfix), "Timeline EnableInput");

        // Unlock screen hooks
        PatchIfFound(harmony, typeof(NUnlockScreen), "Open",
            nameof(UnlockScreenOpenPostfix), "Unlock screen Open");

        // Epoch inspect hooks
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "Open",
            nameof(EpochInspectOpenPostfix), "Epoch inspect Open");
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "OpenViaPaginator",
            nameof(EpochPaginatePostfix), "Epoch paginate");
        PatchIfFound(harmony, typeof(NEpochInspectScreen), "Close",
            nameof(EpochInspectClosedPostfix), "Epoch inspect Close");

        // Settings screen hooks (OnSubmenuOpened/Closed work for both main menu and pause)
        PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuOpened",
            nameof(SettingsOpenedPostfix), "Settings OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NSettingsScreen), "OnSubmenuClosed",
            nameof(SettingsClosedPostfix), "Settings OnSubmenuClosed");

        // Card pile screen hooks
        PatchIfFound(harmony, typeof(NCardPileScreen), "ShowScreen",
            nameof(CardPileShowPostfix), "CardPile ShowScreen");
        PatchIfFound(harmony, typeof(NCardPileScreen), "AfterCapstoneClosed",
            nameof(CardPileClosedPostfix), "CardPile AfterCapstoneClosed");
        PatchIfFound(harmony, typeof(NDeckViewScreen), "ShowScreen",
            nameof(DeckViewShowPostfix), "DeckView ShowScreen");
        PatchIfFound(harmony, typeof(NDeckViewScreen), "AfterCapstoneClosed",
            nameof(DeckViewClosedPostfix), "DeckView AfterCapstoneClosed");

        // Hand card selection hooks
        PatchIfFound(harmony, typeof(NPlayerHand), "SelectCards",
            nameof(SelectCardsPostfix), "Hand SelectCards");
        PatchIfFound(harmony, typeof(NPlayerHand), "AfterCardsSelected",
            nameof(HandSelectClosedPostfix), "Hand AfterCardsSelected");

        // Overlay stack hooks (card grid selection screens)
        PatchIfFound(harmony, typeof(NOverlayStack), "Push",
            nameof(OverlayPushPostfix), "Overlay Push");
        PatchIfFound(harmony, typeof(NOverlayStack), "Remove",
            nameof(OverlayRemovePostfix), "Overlay Remove");

        // Targeting focus (foul potion in shop)
        var startTargeting = AccessTools.Method(typeof(NTargetManager), "StartTargeting",
            new[] { typeof(TargetType), typeof(Vector2), typeof(TargetMode), typeof(System.Func<bool>), typeof(System.Func<Node, bool>) });
        if (startTargeting != null)
        {
            harmony.Patch(startTargeting,
                postfix: new HarmonyMethod(typeof(ScreenHooks), nameof(StartTargetingPostfix)));
            Log.Info("[AccessibilityMod] StartTargeting hook patched.");
        }

        // Bundle preview focus setup
        PatchIfFound(harmony, typeof(NChooseABundleSelectionScreen), "OnBundleClicked",
            nameof(BundlePreviewPostfix), "Bundle preview focus");
        PatchIfFound(harmony, typeof(NChooseABundleSelectionScreen), "CancelSelection",
            nameof(BundleCancelPostfix), "Bundle cancel focus");

        // Character select hooks
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuOpened",
            nameof(CharacterSelectOpenedPostfix), "CharacterSelect OnSubmenuOpened");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnSubmenuClosed",
            nameof(CharacterSelectClosedPostfix), "CharacterSelect OnSubmenuClosed");

        // Multiplayer lobby hooks (IStartRunLobbyListener on NCharacterSelectScreen)
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "PlayerConnected",
            nameof(LobbyPlayerConnectedPostfix), "Lobby PlayerConnected");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "PlayerChanged",
            nameof(LobbyPlayerChangedPostfix), "Lobby PlayerChanged");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "RemotePlayerDisconnected",
            nameof(LobbyPlayerDisconnectedPostfix), "Lobby RemotePlayerDisconnected");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "LocalPlayerDisconnected",
            nameof(LobbyLocalDisconnectedPostfix), "Lobby LocalPlayerDisconnected");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnEmbarkPressed",
            nameof(LobbyEmbarkPostfix), "Lobby OnEmbarkPressed");
        PatchIfFound(harmony, typeof(NCharacterSelectScreen), "OnUnreadyPressed",
            nameof(LobbyUnreadyPostfix), "Lobby OnUnreadyPressed");

        // Map screen hooks
        PatchIfFound(harmony, typeof(NMapScreen), "Open",
            nameof(MapScreenOpenPostfix), "MapScreen Open");
        PatchIfFound(harmony, typeof(NMapScreen), "Close",
            nameof(MapScreenClosePostfix), "MapScreen Close");

        // Rest site hooks
        PatchIfFound(harmony, typeof(NRestSiteRoom), "_Ready",
            nameof(RestSiteReadyPostfix), "RestSite _Ready");
        PatchIfFound(harmony, typeof(NRestSiteRoom), "_ExitTree",
            nameof(RestSiteExitPostfix), "RestSite _ExitTree");

        // Run lifecycle hooks
        PatchIfFound(harmony, typeof(RunManager), "Launch",
            nameof(RunLaunchPostfix), "Run Launch");
        PatchIfFound(harmony, typeof(RunManager), "OnEnded",
            nameof(RunEndedPostfix), "Run OnEnded");
        PatchIfFound(harmony, typeof(RunManager), "CleanUp",
            nameof(RunCleanUpPrefix), "Run CleanUp", isPrefix: true);
    }

    private static void PatchIfFound(Harmony harmony, System.Type type, string methodName,
        string handlerName, string label, bool isPrefix = false)
    {
        var method = AccessTools.Method(type, methodName);
        if (method == null) return;

        var handler = new HarmonyMethod(typeof(ScreenHooks), handlerName);
        if (isPrefix)
            harmony.Patch(method, prefix: handler);
        else
            harmony.Patch(method, postfix: handler);
        Log.Info($"[AccessibilityMod] {label} hook patched.");
    }

    // Core
    public static void UpdatePostfix() => ScreenManager.OnGameScreenChanged();

    // Game over delegates
    // Banner fires before ActiveScreenContext.Update(), so Current may not exist yet.
    // Ensure the screen is pushed early.
    public static void GameOverBannerPostfix(NGameOverScreen __instance)
    {
        if (GameOverScreen.Current == null)
            ScreenManager.PushScreen(new GameOverScreen());
        GameOverScreen.Current?.OnBannerAndQuote(__instance);
    }

    public static void AddBadgePostfix(string locEntryKey, string? locAmountKey, int amount)
        => GameOverScreen.Current?.OnBadge(locEntryKey, locAmountKey, amount);

    public static void AnimateScoreBarPrefix(NGameOverScreen __instance)
        => GameOverScreen.Current?.OnScore(__instance);

    // Timeline delegates
    public static void TimelineEnableInputPostfix()
        => TimelineGameScreen.Current?.OnEnableInput();

    public static void UnlockScreenOpenPostfix(NUnlockScreen __instance)
        => TimelineGameScreen.Current?.OnUnlockScreenOpen(__instance);

    // Epoch inspect delegates
    public static void EpochInspectOpenPostfix(EpochModel epoch, bool wasRevealed)
    {
        if (EpochInspectScreen.Current == null)
            ScreenManager.PushScreen(new EpochInspectScreen());
        EpochInspectScreen.Current?.OnOpen(epoch, wasRevealed);
    }

    public static void EpochPaginatePostfix(EpochModel epoch)
        => EpochInspectScreen.Current?.OnPaginate(epoch);

    public static void EpochInspectClosedPostfix()
    {
        if (EpochInspectScreen.Current != null)
            ScreenManager.RemoveScreen(EpochInspectScreen.Current);
    }

    // Settings delegates
    public static void SettingsOpenedPostfix(NSettingsScreen __instance)
    {
        // OnSubmenuOpened fires before ActiveScreenContext.Update(), so we push
        // here and flag it so OnGameScreenChanged doesn't interfere.
        if (SettingsGameScreen.Current == null)
        {
            var screen = new SettingsGameScreen(__instance);
            ScreenManager.PushScreen(screen);
        }
    }

    public static void SettingsClosedPostfix()
    {
        if (SettingsGameScreen.Current != null)
            ScreenManager.RemoveScreen(SettingsGameScreen.Current);
    }

    // Card pile delegates
    public static void CardPileShowPostfix(NCardPileScreen __result)
    {
        if (CardPileGameScreen.Current == null)
            ScreenManager.PushScreen(new CardPileGameScreen(__result));
    }

    public static void CardPileClosedPostfix(NCardPileScreen __instance)
    {
        if (CardPileGameScreen.Current != null)
            ScreenManager.RemoveScreen(CardPileGameScreen.Current);
    }

    public static void DeckViewShowPostfix(NDeckViewScreen __result)
    {
        if (__result != null && CardPileGameScreen.Current == null)
            ScreenManager.PushScreen(new CardPileGameScreen(__result));
    }

    public static void DeckViewClosedPostfix(NDeckViewScreen __instance)
    {
        if (CardPileGameScreen.Current != null)
            ScreenManager.RemoveScreen(CardPileGameScreen.Current);
    }

    // Hand card selection delegates
    public static void SelectCardsPostfix(NPlayerHand __instance, CardSelectorPrefs prefs)
    {
        if (HandSelectGameScreen.Current != null) return;

        string label = "Card Selection";
        try
        {
            var text = prefs.Prompt.GetFormattedText();
            if (!string.IsNullOrEmpty(text))
                label = text;
        }
        catch { }

        var handScreen = new HandSelectGameScreen(__instance, label);
        if (CombatScreen.Current != null)
            CombatScreen.Current.PushChild(handScreen);
        else
            ScreenManager.PushScreen(handScreen);
        SpeechManager.Output(Message.Raw(label));
    }

    public static void HandSelectClosedPostfix()
    {
        if (HandSelectGameScreen.Current != null)
            ScreenManager.RemoveFromTree(HandSelectGameScreen.Current);
    }

    // Overlay delegates (card grid selection screens)
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
    }

    // Map screen delegates
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

    // Character select delegates
    public static void CharacterSelectOpenedPostfix(NCharacterSelectScreen __instance)
    {
        if (CharacterSelectGameScreen.Current == null)
            ScreenManager.PushScreen(new CharacterSelectGameScreen(__instance));
    }

    public static void CharacterSelectClosedPostfix()
    {
        if (CharacterSelectGameScreen.Current != null)
            ScreenManager.RemoveScreen(CharacterSelectGameScreen.Current);
    }

    // Multiplayer lobby delegates
    public static void LobbyPlayerConnectedPostfix(NCharacterSelectScreen __instance, LobbyPlayer player)
    {
        CharacterSelectGameScreen.Current?.OnLobbyPlayerConnected(__instance, player);
    }

    public static void LobbyPlayerChangedPostfix(NCharacterSelectScreen __instance, LobbyPlayer player)
    {
        CharacterSelectGameScreen.Current?.OnLobbyPlayerChanged(__instance, player);
    }

    public static void LobbyPlayerDisconnectedPostfix(NCharacterSelectScreen __instance, LobbyPlayer player)
    {
        CharacterSelectGameScreen.Current?.OnLobbyPlayerDisconnected(__instance, player);
    }

    public static void LobbyLocalDisconnectedPostfix(NCharacterSelectScreen __instance, NetErrorInfo info)
    {
        CharacterSelectGameScreen.Current?.OnLobbyLocalDisconnected(__instance, info);
    }

    public static void LobbyEmbarkPostfix(NCharacterSelectScreen __instance)
    {
        CharacterSelectGameScreen.Current?.OnLobbyStateChanged();
    }

    public static void LobbyUnreadyPostfix(NCharacterSelectScreen __instance)
    {
        CharacterSelectGameScreen.Current?.OnLobbyStateChanged();
    }

    // Rest site delegates
    public static void RestSiteReadyPostfix(NRestSiteRoom __instance)
    {
        if (RestSiteGameScreen.Current == null)
            ScreenManager.PushScreen(new RestSiteGameScreen(__instance));
    }

    public static void RestSiteExitPostfix()
    {
        if (RestSiteGameScreen.Current != null)
            ScreenManager.RemoveScreen(RestSiteGameScreen.Current);
    }

    // Run lifecycle delegates
    public static void RunLaunchPostfix(RunState __result)
    {
        if (RunScreen.Current == null)
            ScreenManager.PushScreen(new RunScreen());
    }

    public static void RunEndedPostfix()
    {
        CombatEventManager.CleanUp();
        if (RunScreen.Current != null)
            ScreenManager.RemoveScreen(RunScreen.Current);
    }

    /// <summary>
    /// RunManager.CleanUp is called by save-and-quit (ReturnToMainMenu) and does
    /// NOT call OnEnded, so we need a separate hook to pop our screens.
    /// Using a prefix so CombatManager.Instance is still alive for unsubscription.
    /// </summary>
    public static void RunCleanUpPrefix()
    {
        CombatEventManager.CleanUp();
        if (RunScreen.Current != null)
            ScreenManager.RemoveScreen(RunScreen.Current);
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
            SpeechManager.Output(Message.Raw("Merchant"));
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
            // Get the preview cards container via reflection
            var cardsField = AccessTools.Field(typeof(NChooseABundleSelectionScreen), "_bundlePreviewCards");
            var previewCards = cardsField?.GetValue(__instance) as Control;
            if (previewCards == null || previewCards.GetChildCount() == 0) return;

            // Disable focus on the hidden bundle row hitboxes so they can't steal focus
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

            // Build set of CardModels from the selected bundle
            var bundleCards = new System.Collections.Generic.HashSet<CardModel>();
            foreach (var card in bundleNode.Bundle)
                bundleCards.Add(card);

            // Only include holders whose card belongs to the selected bundle
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

            // Set up focus neighbors on the holders (wrap around)
            for (int i = 0; i < holders.Count; i++)
            {
                var left = i > 0 ? holders[i - 1] : holders[holders.Count - 1];
                var right = i < holders.Count - 1 ? holders[i + 1] : holders[0];
                holders[i].FocusNeighborLeft = left.GetPath();
                holders[i].FocusNeighborRight = right.GetPath();
                holders[i].FocusNeighborTop = holders[i].GetPath();
                holders[i].FocusNeighborBottom = holders[i].GetPath();
            }

            // Focus the first holder
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
            // Clear focus neighbors and disable focus on preview card holders
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

            // Re-enable focus on bundle row hitboxes
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
}
