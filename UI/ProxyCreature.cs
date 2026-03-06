using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Sts2AccessibilityMod.Buffers;

namespace Sts2AccessibilityMod.UI;

public class ProxyCreature : ProxyElement
{
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

    public override string? GetLabel()
    {
        var entity = GetEntity();
        if (entity == null) return CleanNodeName(Control.Name);
        return entity.Name;
    }

    public override string? GetStatusString()
    {
        var entity = GetEntity();
        if (entity == null) return null;

        var parts = new List<string>();

        // HP
        parts.Add($"{entity.CurrentHp}/{entity.MaxHp} HP");

        // Block
        if (entity.Block > 0)
            parts.Add($"{entity.Block} block");

        // Intent summary for monsters
        if (entity.IsMonster && entity.Monster != null)
        {
            var intentSummary = GetIntentSummary(entity);
            if (!string.IsNullOrEmpty(intentSummary))
                parts.Add(intentSummary);
        }

        return string.Join(", ", parts);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var entity = GetEntity();
        if (entity == null) return base.HandleBuffers(buffers);

        // If this is the local player, use the player buffer instead
        if (LocalContext.IsMe(entity))
        {
            PlayerBufferHelper.Populate(buffers);
            return "player";
        }

        var creatureBuffer = buffers.GetBuffer("creature");
        if (creatureBuffer != null)
        {
            creatureBuffer.Clear();

            // Name
            creatureBuffer.Add(entity.Name);

            // HP
            creatureBuffer.Add($"HP: {entity.CurrentHp}/{entity.MaxHp}");

            // Block
            if (entity.Block > 0)
                creatureBuffer.Add($"Block: {entity.Block}");

            // Intents for monsters
            if (entity.IsMonster && entity.Monster != null)
            {
                try
                {
                    var intents = entity.Monster.NextMove.Intents;
                    if (intents != null && intents.Count > 0)
                    {
                        var allies = entity.CombatState?.Allies;
                        foreach (var intent in intents)
                        {
                            var tip = intent.GetHoverTip(allies ?? Enumerable.Empty<Creature>(), entity);
                            var intentText = tip.Title ?? intent.IntentType.ToString();
                            if (!string.IsNullOrEmpty(tip.Description))
                                intentText += ": " + StripBbcode(tip.Description);
                            creatureBuffer.Add(intentText);
                        }
                    }
                }
                catch
                {
                    // Intent access may fail outside combat
                }
            }

            // Powers (buffs/debuffs)
            if (entity.Powers.Count > 0)
            {
                foreach (var power in entity.Powers)
                {
                    var title = power.Title.GetFormattedText();
                    var amount = power.Amount;
                    var powerText = amount != 0 ? $"{title} {amount}" : title;
                    creatureBuffer.Add(powerText);
                }
            }

            buffers.EnableBuffer("creature", true);
        }

        // Also populate the player buffer for non-player creatures
        PlayerBufferHelper.Populate(buffers);

        return "creature";
    }

    private static string? GetIntentSummary(Creature entity)
    {
        try
        {
            var intents = entity.Monster?.NextMove?.Intents;
            if (intents == null || intents.Count == 0) return null;

            var summaries = new List<string>();
            var allies = entity.CombatState?.Allies;

            foreach (var intent in intents)
            {
                var label = intent.GetIntentLabel(allies ?? Enumerable.Empty<Creature>(), entity);
                var text = label.GetFormattedText();
                if (!string.IsNullOrEmpty(text) && text != "")
                    summaries.Add($"{intent.IntentType}: {StripBbcode(text)}");
                else
                    summaries.Add(intent.IntentType.ToString());
            }

            return summaries.Count > 0 ? "Intent: " + string.Join(", ", summaries) : null;
        }
        catch
        {
            return null;
        }
    }
}
