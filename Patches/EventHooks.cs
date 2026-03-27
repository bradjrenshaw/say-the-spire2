using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Rooms;
using SayTheSpire2.Events;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.Patches;

public static class EventHooks
{
    private static readonly FieldInfo? TitleField =
        typeof(NEventLayout).GetField("_title", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? DialogueContainerField =
        typeof(NAncientEventLayout).GetField("_dialogueContainer", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? EventField =
        typeof(NEventLayout).GetField("_event", BindingFlags.Instance | BindingFlags.NonPublic);

    public static void Initialize(Harmony harmony)
    {
        var setDescription = AccessTools.Method(typeof(NEventLayout), "SetDescription");
        if (setDescription != null)
        {
            harmony.Patch(setDescription,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(SetDescriptionPostfix)));
            Log.Info("[AccessibilityMod] Event SetDescription hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NEventLayout.SetDescription!");
        }

        var initVisuals = AccessTools.Method(typeof(NAncientEventLayout), "InitializeVisuals");
        if (initVisuals != null)
        {
            harmony.Patch(initVisuals,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(AncientInitializeVisualsPostfix)));
            Log.Info("[AccessibilityMod] Ancient InitializeVisuals hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NAncientEventLayout.InitializeVisuals!");
        }

        // Card stealing (SwipePower.Steal)
        var stealMethod = AccessTools.Method(typeof(SwipePower), "Steal");
        if (stealMethod != null)
        {
            harmony.Patch(stealMethod,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(CardStolenPostfix)));
            Log.Info("[AccessibilityMod] SwipePower.Steal hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find SwipePower.Steal!");
        }

        // Card upgrade during combat (Apotheosis, Armaments, etc.)
        // NCardUpgradeVfx doesn't fire for combat piles, so we hook CardCmd.Upgrade directly
        // but only announce during combat (CardFactory reward creation happens out of combat)
        var upgradeMethod = AccessTools.Method(typeof(CardCmd), "Upgrade",
            new[] { typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle) });
        if (upgradeMethod != null)
        {
            harmony.Patch(upgradeMethod,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(CardUpgradeCombatPostfix)));
            Log.Info("[AccessibilityMod] CardCmd.Upgrade (combat) hook patched.");
        }

        // Card upgrade VFX — fires when the game shows the upgrade animation (out-of-combat deck upgrades)
        var upgradeVfx = AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardUpgradeVfx), "Create");
        if (upgradeVfx != null)
        {
            harmony.Patch(upgradeVfx,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(CardUpgradeVfxPostfix)));
            Log.Info("[AccessibilityMod] NCardUpgradeVfx.Create hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NCardUpgradeVfx.Create!");
        }

        // Card smith VFX — fires when upgrading at rest site
        var smithVfx = AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardSmithVfx), "Create",
            new[] { typeof(IEnumerable<CardModel>), typeof(bool) });
        if (smithVfx != null)
        {
            harmony.Patch(smithVfx,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(CardSmithVfxPostfix)));
            Log.Info("[AccessibilityMod] NCardSmithVfx.Create hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NCardSmithVfx.Create!");
        }

        // Orb events
        var orbChanneled = AccessTools.Method(typeof(Hook), "AfterOrbChanneled");
        if (orbChanneled != null)
        {
            harmony.Patch(orbChanneled,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(OrbChanneledPostfix)));
            Log.Info("[AccessibilityMod] Hook.AfterOrbChanneled hook patched.");
        }

        var orbEvoked = AccessTools.Method(typeof(Hook), "AfterOrbEvoked");
        if (orbEvoked != null)
        {
            harmony.Patch(orbEvoked,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(OrbEvokedPostfix)));
            Log.Info("[AccessibilityMod] Hook.AfterOrbEvoked hook patched.");
        }

        // Card played (fires as card is being played, on all clients)
        var spendResources = AccessTools.Method(typeof(CardModel), "SpendResources");
        if (spendResources != null)
        {
            harmony.Patch(spendResources,
                prefix: new HarmonyMethod(typeof(EventHooks), nameof(CardPlayedPrefix)));
            Log.Info("[AccessibilityMod] CardModel.SpendResources hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find CardModel.SpendResources!");
        }

        // Potion used (fires as potion is being used, on all clients)
        var onUseWrapper = AccessTools.Method(typeof(PotionModel), "OnUseWrapper");
        if (onUseWrapper != null)
        {
            harmony.Patch(onUseWrapper,
                prefix: new HarmonyMethod(typeof(EventHooks), nameof(PotionUsedPrefix)));
            Log.Info("[AccessibilityMod] PotionModel.OnUseWrapper hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find PotionModel.OnUseWrapper!");
        }

        // Gold changes
        var gainGold = AccessTools.Method(typeof(PlayerCmd), "GainGold");
        if (gainGold != null)
        {
            harmony.Patch(gainGold,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(GainGoldPostfix)));
            Log.Info("[AccessibilityMod] PlayerCmd.GainGold hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find PlayerCmd.GainGold!");
        }

        var loseGold = AccessTools.Method(typeof(PlayerCmd), "LoseGold");
        if (loseGold != null)
        {
            harmony.Patch(loseGold,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(LoseGoldPostfix)));
            Log.Info("[AccessibilityMod] PlayerCmd.LoseGold hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find PlayerCmd.LoseGold!");
        }

        // Empty treasure chest
        var initRelics = AccessTools.Method(typeof(NTreasureRoomRelicCollection), "InitializeRelics");
        if (initRelics != null)
        {
            harmony.Patch(initRelics,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(InitializeRelicsPostfix)));
            Log.Info("[AccessibilityMod] NTreasureRoomRelicCollection.InitializeRelics hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NTreasureRoomRelicCollection.InitializeRelics!");
        }

        // Room entered
        var afterRoomEntered = AccessTools.Method(typeof(Hook), "AfterRoomEntered");
        if (afterRoomEntered != null)
        {
            harmony.Patch(afterRoomEntered,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(RoomEnteredPostfix)));
            Log.Info("[AccessibilityMod] Hook.AfterRoomEntered hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find Hook.AfterRoomEntered!");
        }

        var setDialogueLine = AccessTools.Method(typeof(NAncientEventLayout), "SetDialogueLineAndAnimate");
        if (setDialogueLine != null)
        {
            harmony.Patch(setDialogueLine,
                postfix: new HarmonyMethod(typeof(EventHooks), nameof(SetDialogueLinePostfix)));
            Log.Info("[AccessibilityMod] Ancient dialogue line hook patched.");
        }
        else
        {
            Log.Error("[AccessibilityMod] Could not find NAncientEventLayout.SetDialogueLineAndAnimate!");
        }
    }

    public static void CardUpgradeCombatPostfix(IEnumerable<CardModel> cards)
    {
        try
        {
            // Only announce during combat — out-of-combat upgrades are handled by VFX hooks
            if (!MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress) return;

            foreach (var card in cards)
            {
                var name = card.Title;
                if (!string.IsNullOrEmpty(name))
                    EventDispatcher.Enqueue(new CardUpgradeEvent(name));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade combat hook error: {e.Message}");
        }
    }

    public static void CardUpgradeVfxPostfix(CardModel card)
    {
        try
        {
            var name = card.Title;
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new CardUpgradeEvent(name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade VFX hook error: {e.Message}");
        }
    }

    public static void CardSmithVfxPostfix(IEnumerable<CardModel> cards)
    {
        try
        {
            foreach (var card in cards)
            {
                var name = card.Title;
                if (!string.IsNullOrEmpty(name))
                    EventDispatcher.Enqueue(new CardUpgradeEvent(name));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card smith VFX hook error: {e.Message}");
        }
    }

    public static void CardStolenPostfix(SwipePower __instance, CardModel card)
    {
        try
        {
            var cardName = card?.Title;
            if (!string.IsNullOrEmpty(cardName))
                CombatScreen.Current?.OnCardStolen(cardName);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card stolen hook error: {e.Message}");
        }
    }

    public static void AncientInitializeVisualsPostfix(NAncientEventLayout __instance)
    {
        try
        {
            var eventModel = EventField?.GetValue(__instance) as AncientEventModel;
            if (eventModel == null) return;

            var title = eventModel.Title?.GetFormattedText();
            var epithet = eventModel.Epithet?.GetFormattedText();

            var text = !string.IsNullOrEmpty(epithet)
                ? $"{title}, {epithet}"
                : title;

            if (!string.IsNullOrEmpty(text))
            {
                Log.Info($"[AccessibilityMod] Ancient event: \"{text}\"");
                SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Ancient InitializeVisuals hook error: {e.Message}");
        }
    }

    public static void SetDialogueLinePostfix(NAncientEventLayout __instance, int lineIndex)
    {
        try
        {
            var container = DialogueContainerField?.GetValue(__instance) as Node;
            if (container == null) return;

            var child = container.GetChildOrNull<Control>(lineIndex);
            if (child == null) return;

            // NAncientDialogueLine has a %Text (MegaRichTextLabel extends RichTextLabel)
            var textNode = child.GetNodeOrNull<RichTextLabel>("%Text");
            if (textNode == null) return;

            var text = textNode.Text;
            if (!string.IsNullOrEmpty(text))
            {
                Log.Info($"[AccessibilityMod] Ancient dialogue: \"{text}\"");
                SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Ancient dialogue hook error: {e.Message}");
        }
    }

    public static void OrbChanneledPostfix(OrbModel orb)
    {
        try
        {
            var name = orb?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new OrbEvent(OrbEventType.Channeled, name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Orb channeled hook error: {e.Message}");
        }
    }

    public static void OrbEvokedPostfix(OrbModel orb)
    {
        try
        {
            var name = orb?.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(name))
                EventDispatcher.Enqueue(new OrbEvent(OrbEventType.Evoked, name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Orb evoked hook error: {e.Message}");
        }
    }

    public static void PotionUsedPrefix(PotionModel __instance)
    {
        try
        {
            var player = __instance.Owner;
            if (player == null) return;
            var playerName = player.Creature != null
                ? Multiplayer.MultiplayerHelper.GetCreatureName(player.Creature)
                : Multiplayer.MultiplayerHelper.GetPlayerName(player.NetId);
            var potionName = __instance.Title.GetFormattedText();
            if (!string.IsNullOrEmpty(potionName))
                EventDispatcher.Enqueue(new PotionUsedEvent(playerName, potionName, player.Creature));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Potion used hook error: {e.Message}");
        }
    }

    public static void CardPlayedPrefix(CardModel __instance)
    {
        try
        {
            var player = __instance.Owner;
            if (player == null) return;
            var playerName = player.Creature != null
                ? Multiplayer.MultiplayerHelper.GetCreatureName(player.Creature)
                : Multiplayer.MultiplayerHelper.GetPlayerName(player.NetId);
            var cardName = __instance.Title;
            if (!string.IsNullOrEmpty(cardName))
                EventDispatcher.Enqueue(new CardPlayedEvent(playerName, cardName, player.Creature));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card played hook error: {e.Message}");
        }
    }

    public static void GainGoldPostfix(decimal amount, Player player)
    {
        try
        {
            int gained = (int)amount;
            if (gained > 0)
            {
                int newGold = player.Gold;
                EventDispatcher.Enqueue(new GoldEvent(newGold - gained, newGold, player.Creature));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Gold gained hook error: {e.Message}");
        }
    }

    public static void LoseGoldPostfix(decimal amount, Player player)
    {
        try
        {
            int lost = (int)amount;
            if (lost > 0)
            {
                int newGold = player.Gold;
                EventDispatcher.Enqueue(new GoldEvent(newGold + lost, newGold, player.Creature));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Gold lost hook error: {e.Message}");
        }
    }

    public static void InitializeRelicsPostfix(NTreasureRoomRelicCollection __instance)
    {
        try
        {
            var field = AccessTools.Field(typeof(NTreasureRoomRelicCollection), "_isEmptyChest");
            if (field != null && field.GetValue(__instance) is true)
            {
                var text = new MegaCrit.Sts2.Core.Localization.LocString("gameplay_ui", "TREASURE_EMPTY").GetFormattedText();
                if (!string.IsNullOrEmpty(text))
                    SpeechManager.Output(Message.Raw(text));
            }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Empty chest hook error: {e.Message}");
        }
    }

    public static void RoomEnteredPostfix(AbstractRoom room)
    {
        try
        {
            EventDispatcher.Enqueue(new RoomEnteredEvent(room.RoomType));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Room entered hook error: {e.Message}");
        }
    }

    public static void SetDescriptionPostfix(NEventLayout __instance, string description)
    {
        try
        {
            var title = "";
            var titleLabel = TitleField?.GetValue(__instance);
            if (titleLabel != null)
            {
                var textProp = titleLabel.GetType().GetProperty("Text");
                if (textProp != null)
                    title = textProp.GetValue(titleLabel) as string ?? "";
            }

            if (string.IsNullOrEmpty(description)) return;

            var prefix = "";
            try
            {
                var eventModel = EventField?.GetValue(__instance) as MegaCrit.Sts2.Core.Models.EventModel;
                if (eventModel?.IsShared == true)
                    prefix = "Shared event. ";
            }
            catch (System.Exception e) { Log.Error($"[AccessibilityMod] Event shared status check failed: {e.Message}"); }

            var text = prefix + (string.IsNullOrEmpty(title) ? description : $"{title}. {description}");
            Log.Info($"[AccessibilityMod] Event description: \"{text}\"");
            SpeechManager.Output(Message.Raw(text));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Event description hook error: {e.Message}");
        }
    }
}
