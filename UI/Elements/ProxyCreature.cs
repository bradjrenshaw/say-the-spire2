using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SayTheSpire2.Buffers;
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

    public override string? GetLabel()
    {
        var entity = GetEntity();
        if (entity == null) return CleanNodeName(Control.Name);
        return entity.Name;
    }

    public override string? GetTypeKey() => "creature";

    public override string? GetStatusString()
    {
        var entity = GetEntity();
        if (entity == null) return null;

        var parts = new List<string>();
        var intentFirst = ModSettings.GetValue<bool>("ui.creature.intent_first");

        string? intentSummary = null;
        if (entity.IsMonster && entity.Monster != null)
            intentSummary = GetIntentSummary(entity, includePrefix: !intentFirst);

        if (intentFirst && !string.IsNullOrEmpty(intentSummary))
            parts.Add(intentSummary);

        // HP
        parts.Add($"{entity.CurrentHp}/{entity.MaxHp} HP");

        // Block
        if (entity.Block > 0)
            parts.Add($"{entity.Block} block");

        if (!intentFirst && !string.IsNullOrEmpty(intentSummary))
            parts.Add(intentSummary);

        return string.Join(", ", parts);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var entity = GetEntity();
        if (entity == null) return base.HandleBuffers(buffers);

        // If this is the local player, use the player buffer (always-enabled by RunScreen)
        if (LocalContext.IsMe(entity))
            return "player";

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

    private static string? GetIntentSummary(Creature entity, bool includePrefix = true)
    {
        try
        {
            var intents = entity.Monster?.NextMove?.Intents;
            if (intents == null || intents.Count == 0) return null;

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

            if (summaries.Count == 0) return null;
            var joined = string.Join(", ", summaries);
            return includePrefix ? "Intent " + joined : joined;
        }
        catch
        {
            return null;
        }
    }
}
