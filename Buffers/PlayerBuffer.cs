using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.UI;

namespace SayTheSpire2.Buffers;

public class PlayerBuffer : Buffer
{
    public PlayerBuffer() : base("player") { }

    public override void Update()
    {
        Repopulate(Populate);
    }

    private void Populate()
    {
        if (!RunManager.Instance.IsInProgress) return;

        try
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return;

            var player = LocalContext.GetMe(runState);
            if (player == null) return;

            var creature = player.Creature;
            var pcs = player.PlayerCombatState;

            Add($"HP: {creature.CurrentHp}/{creature.MaxHp}");

            if (creature.Block > 0)
                Add($"Block: {creature.Block}");

            if (pcs != null)
                Add(ResourceHelper.GetResourceString(pcs));

            Add($"Gold: {player.Gold}");

            if (pcs != null)
            {
                var piles = $"Draw: {pcs.DrawPile.Cards.Count}, Hand: {pcs.Hand.Cards.Count}, Discard: {pcs.DiscardPile.Cards.Count}";
                if (pcs.ExhaustPile.Cards.Count > 0)
                    piles += $", Exhaust: {pcs.ExhaustPile.Cards.Count}";
                Add(piles);
            }

            if (creature.Powers.Count > 0)
            {
                foreach (var power in creature.Powers)
                    AddPowerToBuffer(this, power);
            }
        }
        catch
        {
            // Combat state may not be accessible
        }
    }

    public static void AddPowerToBuffer(Buffer buffer, PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        var amount = power.Amount;
        var line = amount != 0 ? $"{title} {amount}" : title;
        try
        {
            bool first = true;
            foreach (var tip in power.HoverTips)
            {
                if (tip is HoverTip ht)
                {
                    var desc = ht.Description;
                    if (first)
                    {
                        if (!string.IsNullOrEmpty(desc))
                            line += ": " + desc;
                        buffer.Add(line);
                        first = false;
                    }
                    else
                    {
                        var extraTitle = ht.Title;
                        var extraLine = !string.IsNullOrEmpty(extraTitle) && !string.IsNullOrEmpty(desc)
                            ? $"{extraTitle}: {desc}"
                            : !string.IsNullOrEmpty(extraTitle) ? extraTitle
                            : desc;
                        buffer.Add(extraLine);
                    }
                }
            }
            if (first)
                buffer.Add(line);
        }
        catch
        {
            buffer.Add(line);
        }
    }
}
