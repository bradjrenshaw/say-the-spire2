using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyRunHistoryMapPoint : ProxyElement
{
    private static readonly FieldInfo? EntryField =
        AccessTools.Field(typeof(NMapPointHistoryEntry), "_entry");
    private static readonly FieldInfo? QuestIconField =
        AccessTools.Field(typeof(NMapPointHistoryEntry), "_questIcon");
    private static readonly FieldInfo? PlayerField =
        AccessTools.Field(typeof(NMapPointHistoryEntry), "_player");

    public ProxyRunHistoryMapPoint(Control control) : base(control) { }

    private NMapPointHistoryEntry? EntryControl => Control as NMapPointHistoryEntry;
    private RunHistoryPlayer? Player => PlayerField?.GetValue(EntryControl) as RunHistoryPlayer;

    public override Message? GetLabel()
    {
        var control = EntryControl;
        var entry = EntryField?.GetValue(control) as MapPointHistoryEntry;
        if (control == null || entry == null)
            return null;

        var room = entry.Rooms.LastOrDefault();
        var text = room == null
            ? Ui("RUN_HISTORY.FLOOR", new { floor = control.FloorNum })
            : Ui("RUN_HISTORY.FLOOR_WITH_ROOM", new { floor = control.FloorNum, room = room.RoomType });
        return Message.Raw(text);
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        var questIcon = QuestIconField?.GetValue(EntryControl) as Control;
        if (questIcon?.Visible == true)
            return Message.Raw(Ui("RUN_HISTORY.QUEST_COMPLETED"));
        return null;
    }

    public override Message? GetTooltip()
    {
        var text = BuildSummary(includeRoomModel: true);
        return text != null ? Message.Raw(text) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null)
            return base.HandleBuffers(buffers);

        uiBuffer.Clear();

        var label = GetLabel()?.Resolve();
        if (!string.IsNullOrWhiteSpace(label))
            uiBuffer.Add(label);

        var status = GetStatusString()?.Resolve();
        if (!string.IsNullOrWhiteSpace(status))
            uiBuffer.Add(status);

        var tooltip = GetTooltip()?.Resolve();
        if (!string.IsNullOrWhiteSpace(tooltip))
            uiBuffer.Add(tooltip);

        var details = GetExpandedDetailItems();
        foreach (var detail in details)
            uiBuffer.Add(detail);

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    public string? GetExpandedDetails()
    {
        var sections = GetExpandedDetailItems();
        return sections.Count > 0 ? string.Join(". ", sections) : null;
    }

    private List<string> GetExpandedDetailItems()
    {
        var control = EntryControl;
        var entry = EntryField?.GetValue(control) as MapPointHistoryEntry;
        var player = Player;
        if (control == null || entry == null || player == null)
            return new List<string>();

        var playerEntry = entry.PlayerStats.FirstOrDefault(stat => stat.PlayerId == player.Id);
        if (playerEntry == null)
            return new List<string>();

        var sections = new List<string>();
        sections.AddRange(BuildActionDetails(playerEntry));
        sections.AddRange(BuildRewardDetails(playerEntry));
        sections.AddRange(BuildSkippedDetails(playerEntry));

        return sections;
    }

    private string? BuildSummary(bool includeRoomModel)
    {
        var control = EntryControl;
        var entry = EntryField?.GetValue(control) as MapPointHistoryEntry;
        var player = Player;
        if (control == null || entry == null || player == null)
            return null;

        var playerEntry = entry.PlayerStats.FirstOrDefault(stat => stat.PlayerId == player.Id);
        if (playerEntry == null)
            return null;

        var parts = new List<string>();
        var room = entry.Rooms.LastOrDefault();
        if (includeRoomModel && room?.ModelId != null)
        {
            var roomTitle = GetRoomModelTitle(room);
            if (!string.IsNullOrWhiteSpace(roomTitle))
                parts.Add(roomTitle);
        }

        parts.Add(Ui("RUN_HISTORY.HP", new { current = playerEntry.CurrentHp, max = playerEntry.MaxHp }));
        parts.Add(Ui("RUN_HISTORY.GOLD", new { gold = playerEntry.CurrentGold }));

        if (playerEntry.DamageTaken > 0)
            parts.Add(Ui("RUN_HISTORY.DAMAGE_TAKEN", new { amount = playerEntry.DamageTaken }));
        if (playerEntry.HpHealed > 0)
            parts.Add(Ui("RUN_HISTORY.HEALED", new { amount = playerEntry.HpHealed }));
        if (playerEntry.MaxHpGained > 0)
            parts.Add(Ui("RUN_HISTORY.MAX_HP_GAINED", new { amount = playerEntry.MaxHpGained }));
        if (playerEntry.MaxHpLost > 0)
            parts.Add(Ui("RUN_HISTORY.MAX_HP_LOST", new { amount = playerEntry.MaxHpLost }));
        if (room != null && room.TurnsTaken > 0)
            parts.Add(Ui("RUN_HISTORY.TURNS", new { amount = room.TurnsTaken }));
        if (playerEntry.GoldGained > 0)
            parts.Add(Ui("RUN_HISTORY.GOLD_GAINED", new { amount = playerEntry.GoldGained }));

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string? GetRoomModelTitle(MapPointRoomHistoryEntry room)
    {
        if (room.ModelId == null)
            return null;

        try
        {
            if (room.RoomType.IsCombatRoom())
                return SaveUtil.EncounterOrDeprecated(room.ModelId).Title.GetFormattedText();
            if (room.RoomType == RoomType.Event)
                return SaveUtil.EventOrDeprecated(room.ModelId).Title.GetFormattedText();
        }
        catch
        {
        }

        return null;
    }

    private static List<string> BuildActionDetails(PlayerMapPointHistoryEntry playerEntry)
    {
        var actions = new List<string>();

        foreach (var ancient in playerEntry.AncientChoices.Where(choice => choice.WasChosen))
            actions.Add(Ui("RUN_HISTORY.CHOSE", new { value = FormatLocString(ancient.Title) }));
        foreach (var ancient in playerEntry.AncientChoices.Where(choice => !choice.WasChosen))
            actions.Add(Ui("RUN_HISTORY.SKIPPED", new { value = FormatLocString(ancient.Title) }));

        foreach (var eventChoice in playerEntry.EventChoices)
            actions.Add(Ui("RUN_HISTORY.CHOSE", new { value = FormatEventChoice(eventChoice) }));

        foreach (var restSiteChoice in playerEntry.RestSiteChoices)
            actions.Add(Ui("RUN_HISTORY.REST_SITE", new { value = restSiteChoice.Replace('_', ' ') }));

        foreach (var quest in playerEntry.CompletedQuests)
            actions.Add(Ui("RUN_HISTORY.QUEST_COMPLETED_ITEM", new { value = FormatValue(SaveUtil.CardOrDeprecated(quest).Title) }));

        foreach (var potion in playerEntry.PotionUsed)
            actions.Add(Ui("RUN_HISTORY.USED_POTION", new { value = FormatValue(SaveUtil.PotionOrDeprecated(potion).Title) }));
        foreach (var potion in playerEntry.PotionDiscarded)
            actions.Add(Ui("RUN_HISTORY.REMOVED_POTION", new { value = FormatValue(SaveUtil.PotionOrDeprecated(potion).Title) }));

        if (playerEntry.GoldSpent > 0)
            actions.Add(Ui("RUN_HISTORY.GOLD_SPENT", new { amount = playerEntry.GoldSpent }));
        if (playerEntry.GoldLost > 0)
            actions.Add(Ui("RUN_HISTORY.GOLD_LOST", new { amount = playerEntry.GoldLost }));
        if (playerEntry.GoldStolen > 0)
            actions.Add(Ui("RUN_HISTORY.GOLD_STOLEN", new { amount = playerEntry.GoldStolen }));

        return actions.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
    }

    private static List<string> BuildRewardDetails(PlayerMapPointHistoryEntry playerEntry)
    {
        var rewards = new List<string>();
        var pickedCardTitles = playerEntry.CardChoices
            .Where(choice => choice.wasPicked)
            .Select(choice => CardModel.FromSerializable(choice.Card).Title)
            .ToHashSet();
        var chosenTitles = playerEntry.AncientChoices
            .Where(choice => choice.WasChosen)
            .Select(choice => FormatLocString(choice.Title))
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .ToHashSet();

        foreach (var choice in playerEntry.CardChoices.Where(choice => choice.wasPicked))
            rewards.Add(Ui("RUN_HISTORY.PICKED_CARD", new { value = CardModel.FromSerializable(choice.Card).Title }));

        foreach (var card in playerEntry.CardsGained)
        {
            var title = CardModel.FromSerializable(card).Title;
            if (!pickedCardTitles.Contains(title))
                rewards.Add(Ui("RUN_HISTORY.OBTAINED_CARD", new { value = title }));
        }

        foreach (var relic in playerEntry.RelicChoices.Where(choice => choice.wasPicked))
        {
            var title = FormatValue(SaveUtil.RelicOrDeprecated(relic.choice).Title);
            // Keep this trim intentionally narrow: only suppress exact "Chose X"
            // duplicates for chosen relic/potion rewards and exact picked/obtained
            // card overlaps. Broader dedupe risks hiding distinct outcomes.
            if (!chosenTitles.Contains(title))
                rewards.Add(Ui("RUN_HISTORY.OBTAINED_RELIC", new { value = title }));
        }

        foreach (var potion in playerEntry.PotionChoices.Where(choice => choice.wasPicked))
        {
            var title = FormatValue(SaveUtil.PotionOrDeprecated(potion.choice).Title);
            if (!chosenTitles.Contains(title))
                rewards.Add(Ui("RUN_HISTORY.OBTAINED_POTION", new { value = title }));
        }

        foreach (var card in playerEntry.CardsRemoved)
            rewards.Add(Ui("RUN_HISTORY.REMOVED_CARD", new { value = CardModel.FromSerializable(card).Title }));
        foreach (var relic in playerEntry.RelicsRemoved)
            rewards.Add(Ui("RUN_HISTORY.REMOVED_RELIC", new { value = FormatValue(SaveUtil.RelicOrDeprecated(relic).Title) }));

        foreach (var card in playerEntry.UpgradedCards)
            rewards.Add(Ui("RUN_HISTORY.UPGRADED", new { value = FormatValue(SaveUtil.CardOrDeprecated(card).Title) }));
        foreach (var card in playerEntry.DowngradedCards)
            rewards.Add(Ui("RUN_HISTORY.DOWNGRADED", new { value = FormatValue(SaveUtil.CardOrDeprecated(card).Title) }));

        foreach (var enchantment in playerEntry.CardsEnchanted)
            rewards.Add(Ui("RUN_HISTORY.ENCHANTED", new
            {
                card = CardModel.FromSerializable(enchantment.Card).Title,
                enchantment = FormatValue(SaveUtil.EnchantmentOrDeprecated(enchantment.Enchantment).Title)
            }));

        foreach (var transformation in playerEntry.CardsTransformed)
            rewards.Add(Ui("RUN_HISTORY.TRANSFORMED", new
            {
                original = CardModel.FromSerializable(transformation.OriginalCard).Title,
                final = CardModel.FromSerializable(transformation.FinalCard).Title
            }));

        return rewards.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
    }

    private static List<string> BuildSkippedDetails(PlayerMapPointHistoryEntry playerEntry)
    {
        var skipped = new List<string>();

        foreach (var choice in playerEntry.CardChoices.Where(choice => !choice.wasPicked))
            skipped.Add(Ui("RUN_HISTORY.SKIPPED_CARD", new { value = CardModel.FromSerializable(choice.Card).Title }));
        foreach (var relic in playerEntry.RelicChoices.Where(choice => !choice.wasPicked))
            skipped.Add(Ui("RUN_HISTORY.SKIPPED_RELIC", new { value = FormatValue(SaveUtil.RelicOrDeprecated(relic.choice).Title) }));
        foreach (var potion in playerEntry.PotionChoices.Where(choice => !choice.wasPicked))
            skipped.Add(Ui("RUN_HISTORY.SKIPPED_POTION", new { value = FormatValue(SaveUtil.PotionOrDeprecated(potion.choice).Title) }));

        return skipped.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
    }

    private static string FormatEventChoice(EventOptionHistoryEntry eventChoice)
    {
        var loc = new LocString(eventChoice.Title.LocTable, eventChoice.Title.LocEntryKey);
        if (eventChoice.Variables != null)
        {
            foreach (var variable in eventChoice.Variables)
                loc.AddObj(variable.Key, variable.Value);
        }

        return FormatLocString(loc);
    }

    private static string FormatLocString(LocString loc)
    {
        return loc.GetFormattedText().Replace('\n', ' ').Trim();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            LocString loc => FormatLocString(loc),
            null => "",
            _ => value.ToString() ?? ""
        };
    }

    private static string Ui(string key, object vars)
    {
        return Message.Localized("ui", key, vars).Resolve();
    }

    private static string Ui(string key)
    {
        return LocalizationManager.GetOrDefault("ui", key, key);
    }
}
