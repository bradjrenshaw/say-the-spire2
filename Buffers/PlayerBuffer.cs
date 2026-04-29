using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.UI;
using SayTheSpire2.Views;

namespace SayTheSpire2.Buffers;

public class PlayerBuffer : Buffer
{
    private Player? _boundPlayer;

    public PlayerBuffer() : base("player") { }

    /// <summary>
    /// Bind to a specific player (e.g., a remote player in multiplayer).
    /// Pass null to revert to local player.
    /// </summary>
    public void Bind(Player? player)
    {
        _boundPlayer = player;
    }

    protected override void ClearBinding()
    {
        _boundPlayer = null;
        Clear();
    }

    public override void Update()
    {
        Repopulate(Populate);
    }

    private void Populate()
    {
        if (!RunManager.Instance.IsInProgress) return;

        try
        {
            var player = _boundPlayer;
            if (player == null)
            {
                var runState = RunManager.Instance.DebugOnlyGetState();
                if (runState == null) return;
                player = LocalContext.GetMe(runState);
            }
            if (player == null) return;

            PopulateForPlayer(player);
        }
        catch (System.Exception e)
        {
            // Combat state may not be accessible
            Log.Info($"[AccessibilityMod] Player buffer populate failed: {e.Message}");
        }
    }

    private void PopulateForPlayer(Player player)
    {
        var creature = player.Creature;
        var pcs = player.PlayerCombatState;

        Add(Message.Localized("ui", "RESOURCE.HP", new { current = creature.CurrentHp, max = creature.MaxHp }).Resolve());

        if (creature.Block > 0)
            Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = creature.Block }).Resolve());

        var facingTargets = CreatureView.FromEntity(creature).SurroundedFacingTargets;
        if (facingTargets.Count > 0)
        {
            var targets = string.Join(", ", facingTargets.Select(c => MultiplayerHelper.GetCreatureName(c)));
            Add(Message.Localized("ui", "CREATURE.FACING", new { targets }).Resolve());
        }

        if (pcs != null)
            Add(ResourceHelper.GetResourceMessage(pcs).Resolve());

        Add(Message.Localized("ui", "RESOURCE.GOLD", new { amount = player.Gold }).Resolve());

        if (pcs != null)
        {
            var piles = Message.Localized("ui", "RESOURCE.DRAW_HAND_DISCARD", new { draw = pcs.DrawPile.Cards.Count, hand = pcs.Hand.Cards.Count, discard = pcs.DiscardPile.Cards.Count });
            if (pcs.ExhaustPile.Cards.Count > 0)
                piles = Message.Join(", ", piles, Message.Localized("ui", "RESOURCE.EXHAUST", new { count = pcs.ExhaustPile.Cards.Count }));
            Add(piles.Resolve());
        }

        if (creature.Powers.Count > 0)
        {
            foreach (var power in creature.Powers)
                AddPowerToBuffer(this, power);
        }
    }

    public static void AddPowerToBuffer(Buffer buffer, PowerModel power)
    {
        var title = power.Title.GetFormattedText();
        var amount = power.DisplayAmount;
        var hasStacks = power.StackType == MegaCrit.Sts2.Core.Entities.Powers.PowerStackType.Counter;
        var line = hasStacks && amount > 0 ? $"{title} {amount}" : title;
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
                else if (tip is CardHoverTip cardTip)
                {
                    if (first)
                    {
                        buffer.Add(line);
                        first = false;
                    }
                    var cardName = cardTip.Card?.Title;
                    if (!string.IsNullOrEmpty(cardName))
                        buffer.Add(cardName);
                }
            }
            if (first)
                buffer.Add(line);
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Power hover tip lookup failed: {e.Message}");
            buffer.Add(line);
        }
    }
}
