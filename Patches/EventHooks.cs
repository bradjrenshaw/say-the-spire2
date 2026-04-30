using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
        AccessTools.Field(typeof(NEventLayout), "_title");

    private static readonly FieldInfo? DialogueContainerField =
        AccessTools.Field(typeof(NAncientEventLayout), "_dialogueContainer");

    private static readonly FieldInfo? EventField =
        AccessTools.Field(typeof(NEventLayout), "_event");

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
        HarmonyHelper.PatchIfFound(harmony, typeof(CardCmd), "Downgrade",
            typeof(EventHooks), nameof(CardDowngradePostfix), "CardCmd.Downgrade");
        HarmonyHelper.PatchIfFound(harmony, typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardUpgradeVfx), "Create",
            typeof(EventHooks), nameof(CardUpgradeVfxPostfix), "NCardUpgradeVfx.Create");
        HarmonyHelper.PatchIfFound(harmony, typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardSmithVfx), "Create",
            typeof(EventHooks), nameof(CardSmithVfxPostfix), "NCardSmithVfx.Create",
            parameterTypes: new[] { typeof(IEnumerable<CardModel>), typeof(bool) });
        // NCardEnchantVfx.Create returns null on test mode and on cards owned
        // by other players, so the postfix's __result filter naturally scopes
        // the announcement to "the player saw the sparkle animation". Matches
        // the project's "if it isn't visually shown we don't report it" rule.
        HarmonyHelper.PatchIfFound(harmony, typeof(MegaCrit.Sts2.Core.Nodes.Vfx.NCardEnchantVfx), "Create",
            typeof(EventHooks), nameof(CardEnchantVfxPostfix), "NCardEnchantVfx.Create");
        HarmonyHelper.PatchIfFound(harmony, typeof(CardModel), "SpendResources",
            typeof(EventHooks), nameof(CardPlayedPrefix), "CardModel.SpendResources", isPrefix: true);
        // CardCmd.PreviewInternal is the chokepoint every public Preview /
        // PreviewCardPileAdd overload routes through, after the local-player +
        // not-test-mode + not-ending guards. Postfix only fires when the
        // preview actually displays.
        HarmonyHelper.PatchIfFound(harmony, typeof(CardCmd), "PreviewInternal",
            typeof(EventHooks), nameof(CardPreviewPostfix), "CardCmd.PreviewInternal");

        // Card discard tracking — mark cards before they hit the discard pile
        HarmonyHelper.PatchIfFound(harmony, typeof(CardCmd), "DiscardAndDraw",
            typeof(EventHooks), nameof(DiscardAndDrawPrefix), "CardCmd.DiscardAndDraw", isPrefix: true);

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

        // Surrounded changes facing without changing power stacks, so creature
        // power events cannot observe it. UpdateDirection carries the target
        // that caused the turn, which is the useful information to announce.
        HarmonyHelper.PatchIfFound(harmony, typeof(SurroundedPower), "UpdateDirection",
            typeof(EventHooks), nameof(SurroundedUpdateDirectionPrefix), "SurroundedPower.UpdateDirection prefix",
            isPrefix: true, parameterTypes: new[] { typeof(Creature) });
        HarmonyHelper.PatchIfFound(harmony, typeof(SurroundedPower), "UpdateDirection",
            typeof(EventHooks), nameof(SurroundedUpdateDirectionPostfix), "SurroundedPower.UpdateDirection postfix",
            parameterTypes: new[] { typeof(Creature) });
    }

    public readonly struct SurroundedFacingState
    {
        public SurroundedFacingState(SurroundedPower.Direction oldFacing, Creature target)
        {
            OldFacing = oldFacing;
            Target = target;
        }

        public SurroundedPower.Direction OldFacing { get; }
        public Creature Target { get; }
    }

    public static void SurroundedUpdateDirectionPrefix(
        SurroundedPower __instance,
        Creature target,
        out SurroundedFacingState __state)
    {
        __state = new SurroundedFacingState(__instance.Facing, target);
    }

    public static void SurroundedUpdateDirectionPostfix(
        SurroundedPower __instance,
        SurroundedFacingState __state,
        ref Task __result)
    {
        try
        {
            __result = AnnounceSurroundedFacingAfterUpdate(__result, __instance, __state);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Surrounded facing hook error: {e.Message}");
        }
    }

    private static async Task AnnounceSurroundedFacingAfterUpdate(
        Task original,
        SurroundedPower power,
        SurroundedFacingState state)
    {
        await original;

        try
        {
            if (power.Facing == state.OldFacing) return;
            EventDispatcher.Enqueue(new SurroundedFacingEvent(power.Owner, state.Target));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Surrounded facing event error: {e.Message}");
        }
    }

    public static void CardUpgradeCombatPostfix(IEnumerable<CardModel> cards)
    {
        try
        {
            // Only announce during combat — out-of-combat upgrades are handled by VFX hooks
            if (!MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress) return;

            foreach (var card in cards)
                EnqueueCardUpgrade(card, isDowngrade: false);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade combat hook error: {e.Message}");
        }
    }

    public static void CardDowngradePostfix(CardModel card)
    {
        try
        {
            EnqueueCardUpgrade(card, isDowngrade: true);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card downgrade hook error: {e.Message}");
        }
    }

    public static void CardUpgradeVfxPostfix(CardModel card)
    {
        try
        {
            EnqueueCardUpgrade(card, isDowngrade: false);
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
                EnqueueCardUpgrade(card, isDowngrade: false);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card smith VFX hook error: {e.Message}");
        }
    }

    public static void CardEnchantVfxPostfix(CardModel card, MegaCrit.Sts2.Core.Nodes.Vfx.NCardEnchantVfx? __result)
    {
        // VFX is suppressed in test mode and for non-local-player cards — Create
        // returns null in those cases. Only announce when the player actually
        // sees the sparkle animation.
        if (__result == null) return;

        try
        {
            var cardName = card.Title;
            var enchantment = card.Enchantment;
            if (string.IsNullOrEmpty(cardName) || enchantment == null) return;

            var enchantmentName = enchantment.Title.GetFormattedText();
            if (string.IsNullOrEmpty(enchantmentName)) return;

            // Match the VFX label visibility: only surface the amount when the
            // game would render it on screen.
            int? amount = enchantment.ShowAmount ? enchantment.DisplayAmount : null;

            EventDispatcher.Enqueue(new CardEnchantedEvent(cardName, enchantmentName, amount));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card enchant VFX hook error: {e.Message}");
        }
    }

    public static void CardPreviewPostfix(CardModel card, System.Threading.Tasks.TaskCompletionSource? __result)
    {
        // PreviewInternal returns null on its early-out paths (test mode, combat ending,
        // not the local player's card, no pile). Skip those — only announce when the
        // preview UI was actually created.
        if (__result == null) return;

        try
        {
            var name = card.Title;
            if (string.IsNullOrEmpty(name)) return;
            EventDispatcher.Enqueue(new CardPreviewEvent(name));
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card preview hook error: {e.Message}");
        }
    }

    private static void EnqueueCardUpgrade(CardModel card, bool isDowngrade)
    {
        var name = card.Title;
        if (string.IsNullOrEmpty(name)) return;

        string? playerName = null;
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? source = null;
        try
        {
            var owner = card.Owner;
            if (owner != null)
            {
                source = owner.Creature;
                playerName = Multiplayer.MultiplayerHelper.GetPlayerDisplayName(owner);
            }
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Card owner lookup failed: {e.Message}");
        }

        EventDispatcher.Enqueue(new CardUpgradeEvent(name, source: source, isDowngrade: isDowngrade, playerName: playerName));
    }

    public static void DiscardAndDrawPrefix(IEnumerable<CardModel> cardsToDiscard)
    {
        try
        {
            foreach (var card in cardsToDiscard)
                CombatCardPileHandlers.OnCardActivelyDiscarded(card);
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] DiscardAndDraw prefix error: {e.Message}");
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
