using System.Collections.Generic;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Rewards;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyRewardButton : ProxyElement
{
    private static readonly FieldInfo? RelicField =
        typeof(RelicReward).GetField("_relic", BindingFlags.Instance | BindingFlags.NonPublic);

    public ProxyRewardButton(Control control) : base(control) { }

    private Reward? GetReward() => (Control as NRewardButton)?.Reward;

    public override string? GetLabel()
    {
        var reward = GetReward();
        if (reward == null) return FindChildText(Control!) ?? CleanNodeName(Control!.Name);
        return reward.Description.GetFormattedText();
    }

    public override string? GetTypeKey() => "button";

    public override string? HandleBuffers(BufferManager buffers)
    {
        var reward = GetReward();
        if (reward == null) return base.HandleBuffers(buffers);

        switch (reward)
        {
            case PotionReward potionReward when potionReward.Potion != null:
                {
                    var inner = ProxyPotionHolder.FromModel(potionReward.Potion);
                    return inner.HandleBuffers(buffers);
                }

            case RelicReward relicReward:
                {
                    var relic = RelicField?.GetValue(relicReward) as RelicModel;
                    if (relic != null)
                    {
                        var inner = ProxyRelicHolder.FromModel(relic);
                        return inner.HandleBuffers(buffers);
                    }

                    // Fallback if reflection fails
                    var uiBuffer = buffers.GetBuffer("ui");
                    if (uiBuffer != null)
                    {
                        uiBuffer.Clear();
                        uiBuffer.Add(reward.Description.GetFormattedText());
                        buffers.EnableBuffer("ui", true);
                    }
                    return "ui";
                }

            case LinkedRewardSet linked:
                {
                    var uiBuffer = buffers.GetBuffer("ui");
                    if (uiBuffer != null)
                    {
                        uiBuffer.Clear();
                        uiBuffer.Add(reward.Description.GetFormattedText());
                        foreach (var sub in linked.Rewards)
                        {
                            uiBuffer.Add(sub.Description.GetFormattedText());
                            AddKeywordTips(uiBuffer, sub.HoverTips, buffers);
                        }
                        buffers.EnableBuffer("ui", true);
                    }
                    return "ui";
                }

            default:
                {
                    var uiBuffer = buffers.GetBuffer("ui");
                    if (uiBuffer != null)
                    {
                        uiBuffer.Clear();
                        uiBuffer.Add(reward.Description.GetFormattedText());
                        AddKeywordTips(uiBuffer, reward.HoverTips, buffers);
                        buffers.EnableBuffer("ui", true);
                    }
                    return "ui";
                }
        }
    }

    /// <summary>
    /// Add hover tips from rewards (rewards don't include a self-referencing first tip).
    /// </summary>
    private static void AddKeywordTips(Buffer buffer, IEnumerable<IHoverTip> tips, BufferManager? buffers = null)
    {
        try
        {
            var cardTips = new List<CardHoverTip>();
            foreach (var tip in tips)
            {
                if (tip is CardHoverTip cardTip)
                {
                    cardTips.Add(cardTip);
                }
                else if (tip is HoverTip ht)
                {
                    var title = ht.Title;
                    var desc = ht.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc))
                        buffer.Add($"{title}: {StripBbcode(desc)}");
                    else if (!string.IsNullOrEmpty(title))
                        buffer.Add(title);
                }
            }

            if (cardTips.Count > 0 && buffers != null)
            {
                var cardBuffer = buffers.GetBuffer("card");
                if (cardBuffer != null)
                {
                    cardBuffer.Clear();
                    foreach (var cardTip in cardTips)
                    {
                        if (cardBuffer.Count > 0)
                            cardBuffer.Add("---");
                        CardBuffer.Populate(cardBuffer, cardTip.Card);
                    }
                    buffers.EnableBuffer("card", true);
                }
            }
        }
        catch (System.Exception e) { Log.Error($"[AccessibilityMod] Reward hover tips access failed: {e.Message}"); }
    }
}
