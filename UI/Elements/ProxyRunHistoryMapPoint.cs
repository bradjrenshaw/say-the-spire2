using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

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

    public override string? GetLabel()
    {
        var control = EntryControl;
        var entry = EntryField?.GetValue(control) as MapPointHistoryEntry;
        if (control == null || entry == null)
            return null;

        var room = entry.Rooms.LastOrDefault();
        return room == null
            ? $"Floor {control.FloorNum}"
            : $"Floor {control.FloorNum}, {room.RoomType}";
    }

    public override string? GetTypeKey() => "button";

    public override string? GetStatusString()
    {
        var questIcon = QuestIconField?.GetValue(EntryControl) as Control;
        return questIcon?.Visible == true ? "Quest completed" : null;
    }

    public override string? GetTooltip()
    {
        return BuildSummary(includeRoomModel: true);
    }

    public string? GetExpandedDetails()
    {
        var control = EntryControl;
        var entry = EntryField?.GetValue(control) as MapPointHistoryEntry;
        var player = Player;
        if (control == null || entry == null || player == null)
            return null;

        var playerEntry = entry.PlayerStats.FirstOrDefault(stat => stat.PlayerId == player.Id);
        if (playerEntry == null)
            return null;

        var sections = new List<string>();
        var actions = BuildActionDetails(entry, playerEntry);
        if (actions.Count > 0)
            sections.Add("Actions: " + string.Join("; ", actions));

        var rewards = BuildRewardDetails(playerEntry, actions);
        if (rewards.Count > 0)
            sections.Add("Rewards: " + string.Join("; ", rewards));

        var skipped = BuildSkippedDetails(playerEntry);
        if (skipped.Count > 0)
            sections.Add("Skipped: " + string.Join("; ", skipped));

        return sections.Count > 0 ? string.Join(". ", sections) : null;
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

        parts.Add($"{playerEntry.CurrentHp}/{playerEntry.MaxHp} HP");
        parts.Add($"{playerEntry.CurrentGold} gold");

        if (playerEntry.DamageTaken > 0)
            parts.Add($"{playerEntry.DamageTaken} damage taken");
        if (playerEntry.HpHealed > 0)
            parts.Add($"{playerEntry.HpHealed} healed");
        if (playerEntry.MaxHpGained > 0)
            parts.Add($"{playerEntry.MaxHpGained} max HP gained");
        if (playerEntry.MaxHpLost > 0)
            parts.Add($"{playerEntry.MaxHpLost} max HP lost");
        if (room != null && room.TurnsTaken > 0)
            parts.Add($"{room.TurnsTaken} turns");
        if (playerEntry.GoldGained > 0)
            parts.Add($"{playerEntry.GoldGained} gold gained");

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

    private static List<string> BuildActionDetails(MapPointHistoryEntry entry, PlayerMapPointHistoryEntry playerEntry)
    {
        var actions = new List<string>();

        foreach (var ancient in playerEntry.AncientChoices.Where(choice => choice.WasChosen))
            actions.Add("Chose " + FormatLocString(ancient.Title));
        foreach (var ancient in playerEntry.AncientChoices.Where(choice => !choice.WasChosen))
            actions.Add("Skipped " + FormatLocString(ancient.Title));

        foreach (var eventChoice in playerEntry.EventChoices)
            actions.Add("Chose " + FormatEventChoice(eventChoice));

        foreach (var restSiteChoice in playerEntry.RestSiteChoices)
            actions.Add("Rest site: " + restSiteChoice.Replace('_', ' '));

        foreach (var quest in playerEntry.CompletedQuests)
            actions.Add("Quest completed: " + FormatValue(SaveUtil.CardOrDeprecated(quest).Title));

        foreach (var potion in playerEntry.PotionUsed)
            actions.Add("Used potion: " + FormatValue(SaveUtil.PotionOrDeprecated(potion).Title));
        foreach (var potion in playerEntry.PotionDiscarded)
            actions.Add("Removed potion: " + FormatValue(SaveUtil.PotionOrDeprecated(potion).Title));

        if (playerEntry.GoldSpent > 0)
            actions.Add($"{playerEntry.GoldSpent} gold spent");
        if (playerEntry.GoldLost > 0)
            actions.Add($"{playerEntry.GoldLost} gold lost");
        if (playerEntry.GoldStolen > 0)
            actions.Add($"{playerEntry.GoldStolen} gold stolen");

        return Deduplicate(actions);
    }

    private static List<string> BuildRewardDetails(PlayerMapPointHistoryEntry playerEntry, IReadOnlyCollection<string> actions)
    {
        var rewards = new List<string>();
        var pickedCardTitles = playerEntry.CardChoices
            .Where(choice => choice.wasPicked)
            .Select(choice => CardModel.FromSerializable(choice.Card).Title)
            .ToHashSet();

        foreach (var choice in playerEntry.CardChoices.Where(choice => choice.wasPicked))
            rewards.Add("Picked card: " + CardModel.FromSerializable(choice.Card).Title);

        foreach (var card in playerEntry.CardsGained)
        {
            var title = CardModel.FromSerializable(card).Title;
            if (!pickedCardTitles.Contains(title))
                rewards.Add("Obtained card: " + title);
        }

        foreach (var relic in playerEntry.RelicChoices.Where(choice => choice.wasPicked))
            rewards.Add("Obtained relic: " + FormatValue(SaveUtil.RelicOrDeprecated(relic.choice).Title));

        foreach (var potion in playerEntry.PotionChoices.Where(choice => choice.wasPicked))
            rewards.Add("Obtained potion: " + FormatValue(SaveUtil.PotionOrDeprecated(potion.choice).Title));

        foreach (var card in playerEntry.CardsRemoved)
            rewards.Add("Removed card: " + CardModel.FromSerializable(card).Title);
        foreach (var relic in playerEntry.RelicsRemoved)
            rewards.Add("Removed relic: " + FormatValue(SaveUtil.RelicOrDeprecated(relic).Title));

        foreach (var card in playerEntry.UpgradedCards)
            rewards.Add("Upgraded: " + FormatValue(SaveUtil.CardOrDeprecated(card).Title));
        foreach (var card in playerEntry.DowngradedCards)
            rewards.Add("Downgraded: " + FormatValue(SaveUtil.CardOrDeprecated(card).Title));

        foreach (var enchantment in playerEntry.CardsEnchanted)
            rewards.Add($"Enchanted: {CardModel.FromSerializable(enchantment.Card).Title} with {FormatValue(SaveUtil.EnchantmentOrDeprecated(enchantment.Enchantment).Title)}");

        foreach (var transformation in playerEntry.CardsTransformed)
            rewards.Add($"Transformed: {CardModel.FromSerializable(transformation.OriginalCard).Title} into {CardModel.FromSerializable(transformation.FinalCard).Title}");

        rewards = Deduplicate(rewards);
        var chosenItems = actions
            .Where(action => action.StartsWith("Chose "))
            .Select(action => action["Chose ".Length..].Trim())
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .ToHashSet();

        if (chosenItems.Count == 0)
            return rewards;

        // Keep this trim intentionally narrow: only remove exact reward lines that
        // restate a spoken "Chose X" item. Broader cross-section dedupe risks
        // hiding meaningful run-history outcomes that are visually distinct.
        return rewards.Where(reward => !IsRedundantChosenReward(reward, chosenItems)).ToList();
    }

    private static List<string> BuildSkippedDetails(PlayerMapPointHistoryEntry playerEntry)
    {
        var skipped = new List<string>();

        foreach (var choice in playerEntry.CardChoices.Where(choice => !choice.wasPicked))
            skipped.Add("Card: " + CardModel.FromSerializable(choice.Card).Title);
        foreach (var relic in playerEntry.RelicChoices.Where(choice => !choice.wasPicked))
            skipped.Add("Relic: " + FormatValue(SaveUtil.RelicOrDeprecated(relic.choice).Title));
        foreach (var potion in playerEntry.PotionChoices.Where(choice => !choice.wasPicked))
            skipped.Add("Potion: " + FormatValue(SaveUtil.PotionOrDeprecated(potion.choice).Title));

        return Deduplicate(skipped);
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

    private static List<string> Deduplicate(IEnumerable<string> values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
                continue;
            result.Add(value);
        }

        return result;
    }

    private static bool IsRedundantChosenReward(string reward, HashSet<string> chosenItems)
    {
        foreach (var prefix in new[]
                 {
                     "Obtained relic: ",
                     "Obtained potion: ",
                     "Picked card: ",
                     "Obtained card: ",
                 })
        {
            if (!reward.StartsWith(prefix))
                continue;

            var item = reward[prefix.Length..].Trim();
            return chosenItems.Contains(item);
        }

        return false;
    }
}
