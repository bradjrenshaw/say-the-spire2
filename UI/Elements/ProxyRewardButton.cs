using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
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
        if (reward == null) return FindChildText(Control) ?? CleanNodeName(Control.Name);
        return reward.Description.GetFormattedText();
    }

    public override string? GetTypeKey() => "button";

    public override string? HandleBuffers(BufferManager buffers)
    {
        var reward = GetReward();
        if (reward == null) return base.HandleBuffers(buffers);

        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null) return "ui";

        uiBuffer.Clear();
        buffers.EnableBuffer("ui", true);

        switch (reward)
        {
            case PotionReward potionReward when potionReward.Potion != null:
                ProxyPotionHolder.PopulatePotionBuffer(uiBuffer, potionReward.Potion);
                break;

            case RelicReward relicReward:
                var relic = RelicField?.GetValue(relicReward) as RelicModel;
                if (relic != null)
                    ProxyRelicHolder.PopulateRelicBuffer(uiBuffer, relic, buffers);
                else
                    uiBuffer.Add(reward.Description.GetFormattedText());
                break;

            case LinkedRewardSet linked:
                uiBuffer.Add(reward.Description.GetFormattedText());
                foreach (var sub in linked.Rewards)
                {
                    uiBuffer.Add(sub.Description.GetFormattedText());
                    AddKeywordTips(uiBuffer, sub.HoverTips);
                }
                break;

            default:
                // Gold, Card, CardRemoval, SpecialCard, etc.
                uiBuffer.Add(reward.Description.GetFormattedText());
                AddKeywordTips(uiBuffer, reward.HoverTips);
                break;
        }

        return "ui";
    }

    /// <summary>
    /// Add hover tips skipping the first (which is the item itself).
    /// </summary>
    private static void AddKeywordTips(Buffer buffer, System.Collections.Generic.IEnumerable<IHoverTip> tips)
    {
        try
        {
            bool first = true;
            foreach (var tip in tips)
            {
                if (first) { first = false; continue; }
                if (tip is HoverTip ht)
                {
                    var title = ht.Title;
                    var desc = ht.Description;
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc))
                        buffer.Add($"{title}: {StripBbcode(desc)}");
                    else if (!string.IsNullOrEmpty(title))
                        buffer.Add(title);
                }
            }
        }
        catch { }
    }
}
