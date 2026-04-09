using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Elements;

[ModSettings("ui.creature", "UI/Creature")]
public class ProxyCreature : ProxyElement
{
    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("intent_first", "Announce Intent Before HP", false));
    }

    public ProxyCreature(Control control) : base(control) { }

    private NCreature? FindCreature()
    {
        if (Control is NCreature direct)
            return direct;
        Node? current = Control.GetParent();
        while (current != null)
        {
            if (current is NCreature creature)
                return creature;
            current = current.GetParent();
        }
        return null;
    }

    private Creature? GetEntity() => FindCreature()?.Entity;

    public override Message? GetLabel()
    {
        var entity = GetEntity();
        if (entity == null) return Message.Raw(CleanNodeName(Control.Name));
        return Message.Raw(MultiplayerHelper.GetCreatureName(entity));
    }

    public override string? GetTypeKey() => "creature";

    public override Message? GetStatusString()
    {
        var entity = GetEntity();
        if (entity == null) return null;

        var parts = new List<string>();
        var intentFirst = ModSettings.GetValue<bool>("ui.creature.intent_first");

        var intentSummary = GetIntentSummary(entity, includePrefix: !intentFirst);

        if (intentFirst && !string.IsNullOrEmpty(intentSummary))
            parts.Add(intentSummary);

        // HP
        parts.Add($"{entity.CurrentHp}/{entity.MaxHp} HP");

        // Block
        if (entity.Block > 0)
            parts.Add($"{entity.Block} block");

        if (!intentFirst && !string.IsNullOrEmpty(intentSummary))
            parts.Add(intentSummary);

        return Message.Raw(string.Join(", ", parts));
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var entity = GetEntity();
        if (entity == null) return base.HandleBuffers(buffers);

        // If this is the local player, use the player buffer (always-enabled by RunScreen)
        if (LocalContext.IsMe(entity))
        {
            var playerBuffer = buffers.GetBuffer("player") as PlayerBuffer;
            if (playerBuffer != null)
            {
                playerBuffer.Bind(null);
                playerBuffer.Update();
                buffers.EnableBuffer("player", true);
            }
            return "player";
        }

        // If this is another player in multiplayer, bind the player buffer to them
        if (entity.IsPlayer && entity.Player != null)
        {
            var playerBuffer = buffers.GetBuffer("player") as PlayerBuffer;
            if (playerBuffer != null)
            {
                playerBuffer.Bind(entity.Player);
                playerBuffer.Update();
                buffers.EnableBuffer("player", true);
            }
            return "player";
        }

        var creatureBuffer = buffers.GetBuffer("creature") as CreatureBuffer;
        if (creatureBuffer != null)
        {
            creatureBuffer.Bind(entity);
            creatureBuffer.Update();
            buffers.EnableBuffer("creature", true);
        }

        return "creature";
    }

    private static readonly PropertyInfo? IntentTitleProp =
        typeof(AbstractIntent).GetProperty("IntentTitle", BindingFlags.Instance | BindingFlags.NonPublic);

    /// <summary>
    /// Gets the game's localized intent title (e.g. from "intents" table).
    /// Falls back to IntentType enum name if reflection fails.
    /// </summary>
    public static string GetIntentName(AbstractIntent intent)
    {
        try
        {
            var locString = IntentTitleProp?.GetValue(intent) as MegaCrit.Sts2.Core.Localization.LocString;
            var title = locString?.GetFormattedText();
            if (!string.IsNullOrEmpty(title))
                return title;
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Intent title lookup failed: {e.Message}"); }
        return intent.IntentType.ToString();
    }

    public static string? GetIntentSummary(Creature entity, bool includePrefix = true)
    {
        try
        {
            if (entity.IsMonster && entity.Monster != null)
                return GetMonsterIntentSummary(entity, includePrefix);

            if (entity.IsPlayer && entity.Player != null)
                return GetPlayerIntentSummary(entity.Player, includePrefix);
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? GetMonsterIntentSummary(Creature entity, bool includePrefix)
    {
        var intents = entity.Monster?.NextMove?.Intents;
        if (intents == null || intents.Count == 0)
            return null;

        var summaries = new List<string>();
        var allies = entity.CombatState?.Allies;

        foreach (var intent in intents)
        {
            var name = GetIntentName(intent);
            var label = intent.GetIntentLabel(allies ?? Enumerable.Empty<Creature>(), entity);
            var text = label.GetFormattedText();
            if (!string.IsNullOrEmpty(text) && text != "")
                summaries.Add($"{name} {StripBbcode(text)}");
            else
                summaries.Add(name);
        }

        if (summaries.Count == 0)
            return null;

        var joined = string.Join(", ", summaries);
        return includePrefix ? "Intent " + joined : joined;
    }

    private static string? GetPlayerIntentSummary(MegaCrit.Sts2.Core.Entities.Players.Player player, bool includePrefix)
    {
        var hoveredModel = RunManager.Instance?.HoveredModelTracker?.GetHoveredModel(player.NetId);
        if (hoveredModel == null)
            return null;

        var summary = GetHoveredModelSummary(hoveredModel);
        if (string.IsNullOrWhiteSpace(summary))
            return null;

        return includePrefix ? "Intent " + summary : summary;
    }

    private static string? GetHoveredModelSummary(AbstractModel model)
    {
        return model switch
        {
            CardModel card => GetCardIntentSummary(card),
            RelicModel relic => GetRelicIntentSummary(relic),
            PotionModel potion => GetPotionIntentSummary(potion),
            PowerModel power => GetPowerIntentSummary(power),
            _ => null,
        };
    }

    private static string GetCardIntentSummary(CardModel card)
    {
        var proxy = ProxyCard.FromModel(card);
        var parts = new List<string>();
        var label = proxy.GetLabel()?.Resolve();
        var extras = proxy.GetExtrasString()?.Resolve();
        var subtype = proxy.GetSubtypeKey();

        if (!string.IsNullOrWhiteSpace(label))
            parts.Add(label);
        if (!string.IsNullOrWhiteSpace(extras))
            parts.Add(extras);
        if (!string.IsNullOrWhiteSpace(subtype))
            parts.Add($"{subtype} card");

        return string.Join(", ", parts);
    }

    private static string GetRelicIntentSummary(RelicModel relic)
    {
        var proxy = ProxyRelicHolder.FromModel(relic);
        var parts = new List<string>();
        var label = proxy.GetLabel()?.Resolve();
        var status = proxy.GetStatusString()?.Resolve();

        if (!string.IsNullOrWhiteSpace(label))
            parts.Add(label);
        if (!string.IsNullOrWhiteSpace(status))
            parts.Add(status);

        return string.Join(", ", parts);
    }

    private static string GetPotionIntentSummary(PotionModel potion)
    {
        return ProxyPotionHolder.FromModel(potion).GetLabel()?.Resolve() ?? potion.Title.GetFormattedText();
    }

    private static string GetPowerIntentSummary(PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        if (power.StackType == PowerStackType.Counter && power.DisplayAmount != 0)
            return $"{title} {power.DisplayAmount}";
        return title;
    }
}
