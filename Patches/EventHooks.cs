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
        // Event descriptions and dialogue
        HarmonyHelper.PatchIfFound(harmony, typeof(NEventLayout), "SetDescription",
            typeof(EventHooks), nameof(SetDescriptionPostfix), "Event SetDescription");
        HarmonyHelper.PatchIfFound(harmony, typeof(NAncientEventLayout), "InitializeVisuals",
            typeof(EventHooks), nameof(AncientInitializeVisualsPostfix), "Ancient InitializeVisuals");
        HarmonyHelper.PatchIfFound(harmony, typeof(NAncientEventLayout), "SetDialogueLineAndAnimate",
            typeof(EventHooks), nameof(SetDialogueLinePostfix), "Ancient dialogue line");

        // Card events
        HarmonyHelper.PatchIfFound(harmony, typeof(SwipePower), "Steal",
            typeof(EventHooks), nameof(CardStolenPostfix), "SwipePower.Steal");
        HarmonyHelper.PatchIfFound(harmony, typeof(CardCmd), "Upgrade",
            typeof(EventHooks), nameof(CardUpgradeCombatPostfix), "CardCmd.Upgrade (combat)",
            parameterTypes: new[] { typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle) });
        HarmonyHelper.PatchIfFound(harmony, typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardUpgradeVfx), "Create",
            typeof(EventHooks), nameof(CardUpgradeVfxPostfix), "NCardUpgradeVfx.Create");
        HarmonyHelper.PatchIfFound(harmony, typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardSmithVfx), "Create",
            typeof(EventHooks), nameof(CardSmithVfxPostfix), "NCardSmithVfx.Create",
            parameterTypes: new[] { typeof(IEnumerable<CardModel>), typeof(bool) });
        HarmonyHelper.PatchIfFound(harmony, typeof(CardModel), "SpendResources",
            typeof(EventHooks), nameof(CardPlayedPrefix), "CardModel.SpendResources", isPrefix: true);

        // Orb events
        HarmonyHelper.PatchIfFound(harmony, typeof(Hook), "AfterOrbChanneled",
            typeof(EventHooks), nameof(OrbChanneledPostfix), "Hook.AfterOrbChanneled");
        HarmonyHelper.PatchIfFound(harmony, typeof(Hook), "AfterOrbEvoked",
            typeof(EventHooks), nameof(OrbEvokedPostfix), "Hook.AfterOrbEvoked");

        // Potion used
        HarmonyHelper.PatchIfFound(harmony, typeof(PotionModel), "OnUseWrapper",
            typeof(EventHooks), nameof(PotionUsedPrefix), "PotionModel.OnUseWrapper", isPrefix: true);

        // Gold changes
        HarmonyHelper.PatchIfFound(harmony, typeof(PlayerCmd), "GainGold",
            typeof(EventHooks), nameof(GainGoldPostfix), "PlayerCmd.GainGold");
        HarmonyHelper.PatchIfFound(harmony, typeof(PlayerCmd), "LoseGold",
            typeof(EventHooks), nameof(LoseGoldPostfix), "PlayerCmd.LoseGold");

        // Treasure and rooms
        HarmonyHelper.PatchIfFound(harmony, typeof(NTreasureRoomRelicCollection), "InitializeRelics",
            typeof(EventHooks), nameof(InitializeRelicsPostfix), "NTreasureRoomRelicCollection.InitializeRelics");
        HarmonyHelper.PatchIfFound(harmony, typeof(Hook), "AfterRoomEntered",
            typeof(EventHooks), nameof(RoomEnteredPostfix), "Hook.AfterRoomEntered");
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
            var playerName = Multiplayer.MultiplayerHelper.GetPlayerDisplayName(player);
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
            var playerName = Multiplayer.MultiplayerHelper.GetPlayerDisplayName(player);
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
                    prefix = LocalizationManager.GetOrDefault("ui", "EVENT.SHARED_PREFIX", "Shared event. ");
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
