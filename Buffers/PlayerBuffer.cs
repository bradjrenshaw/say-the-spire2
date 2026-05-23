using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.Buffers;

[BufferAnnouncementOrder(
    typeof(HpAnnouncement),
    typeof(BlockAnnouncement),
    typeof(FacingAnnouncement),
    typeof(ResourcesAnnouncement),
    typeof(GoldAnnouncement),
    typeof(PilesAnnouncement),
    typeof(PowersAnnouncement)
)]
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

            Populate(this, player);
        }
        catch (Exception e)
        {
            Log.Info($"[AccessibilityMod] Player buffer populate failed: {e.Message}");
        }
    }

    public static void Populate(Buffer buffer, Player player)
    {
        var attrOrder = typeof(PlayerBuffer).GetCustomAttributes(typeof(BufferAnnouncementOrderAttribute), inherit: true)
            is { Length: > 0 } attrs && attrs[0] is BufferAnnouncementOrderAttribute order
            ? order.Types
            : Array.Empty<Type>();

        BufferAnnouncementComposer.Compose(buffer, "player", attrOrder, BuildAnnouncements(player));
    }

    private static IEnumerable<Announcement> BuildAnnouncements(Player player)
    {
        var creature = player.Creature;
        var pcs = player.PlayerCombatState;

        yield return new HpAnnouncement(creature.CurrentHp, creature.MaxHp);

        if (creature.Block > 0)
            yield return new BlockAnnouncement(creature.Block);

        var facingTargets = CreatureView.FromEntity(creature).SurroundedFacingTargets;
        if (facingTargets.Count > 0)
        {
            var targets = string.Join(", ", facingTargets.Select(c => MultiplayerHelper.GetCreatureName(c)));
            yield return new FacingAnnouncement(targets);
        }

        if (pcs != null)
            yield return new ResourcesAnnouncement(pcs);

        yield return new GoldAnnouncement(player.Gold);

        if (pcs != null)
            yield return new PilesAnnouncement(
                pcs.DrawPile.Cards.Count,
                pcs.Hand.Cards.Count,
                pcs.DiscardPile.Cards.Count,
                pcs.ExhaustPile.Cards.Count);

        if (creature.Powers.Count > 0)
            yield return new PowersAnnouncement(creature.Powers);
    }
}
