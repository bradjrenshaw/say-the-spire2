using System.Collections.Generic;
using Godot;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(DyingAnnouncement),
    typeof(OwnerAnnouncement),
    typeof(TypeAnnouncement),
    typeof(HpAnnouncement),
    typeof(BlockAnnouncement),
    typeof(EnergyAnnouncement),
    typeof(StarsAnnouncement),
    typeof(CardsInHandAnnouncement),
    typeof(MonsterIntentsAnnouncement),
    typeof(PlayerIntentsAnnouncement)
)]
public class ProxyCreature : ProxyElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var view = GetView();
        if (view == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        yield return new LabelAnnouncement(view.Name);
        var lethalDot = view.LethalDotTitle;
        if (lethalDot != null)
            yield return new DyingAnnouncement(lethalDot);
        var petOwner = view.OtherPlayerPetOwner;
        if (petOwner != null)
            yield return new OwnerAnnouncement(MultiplayerHelper.GetPlayerName(petOwner));
        yield return new TypeAnnouncement("creature");
        yield return new HpAnnouncement(view.CurrentHp, view.MaxHp);
        if (view.Block > 0)
            yield return new BlockAnnouncement(view.Block);

        if (view.IsMonster)
        {
            yield return new MonsterIntentsAnnouncement(view.MonsterIntents);
        }
        else if (view.IsPlayer)
        {
            var pcs = view.PlayerCombatState;
            if (pcs != null)
            {
                yield return new EnergyAnnouncement(pcs.Energy, pcs.MaxEnergy);
                if (pcs.Stars > 0)
                    yield return new StarsAnnouncement(pcs.Stars);
                yield return new CardsInHandAnnouncement(pcs.Hand.Cards.Count);
            }

            if (view.PlayerHoveredModel != null)
            {
                var summary = CreatureIntentFormatter.HoveredModelSummary(view.PlayerHoveredModel);
                if (summary is { IsEmpty: false })
                    yield return new PlayerIntentsAnnouncement(summary);
            }
        }
    }

    public ProxyCreature(Control control) : base(control) { }

    /// <summary>
    /// NCreature itself is not the focus owner — its child Hitbox
    /// (NClickableControl) is what NCombatRoom wires FocusNeighbors against.
    /// Redirect GrabFocus so jump-to-creature actually moves Godot focus.
    /// </summary>
    public override void GrabFocus()
    {
        if (Control is MegaCrit.Sts2.Core.Nodes.Combat.NCreature creature
            && creature.Hitbox is { } hitbox
            && GodotObject.IsInstanceValid(hitbox))
        {
            hitbox.GrabFocus();
            return;
        }
        base.GrabFocus();
    }

    private CreatureView? GetView() => CreatureView.FromControl(Control);

    /// <summary>
    /// While a card is being aimed, only valid targets are focus-reachable
    /// (the game restricts controller navigation to them), so only they
    /// count toward "2 of 4" positions. Mirrors the game's own targeting
    /// rules from NControllerCardPlay.SingleCreatureTargeting: hittable
    /// opponents for AnyEnemy, hittable non-owner allies for AnyAlly.
    /// </summary>
    public override bool CountsForPosition
    {
        get
        {
            if (!IsVisible)
                return false;
            try
            {
                var card = Screens.CombatScreen.CurrentPlayedCard();
                if (card == null)
                    return true;
                var owner = card.Owner?.Creature;
                var creature = GetView()?.Entity;
                if (owner == null || creature == null)
                    return true;
                return card.TargetType switch
                {
                    MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyEnemy =>
                        creature.IsHittable
                        && owner.CombatState is { } ownerCombat
                        && System.Linq.Enumerable.Contains(ownerCombat.GetOpponentsOf(owner), creature),
                    MegaCrit.Sts2.Core.Entities.Cards.TargetType.AnyAlly =>
                        creature.IsHittable
                        && !ReferenceEquals(creature, owner)
                        && card.CombatState is { } cardCombat
                        && System.Linq.Enumerable.Contains(cardCombat.PlayerCreatures, creature),
                    _ => true,
                };
            }
            catch (System.Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] Target position check failed: {e.Message}");
                return true;
            }
        }
    }

    public override Message? GetLabel()
    {
        var view = GetView();
        if (view == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        return Message.Raw(view.Name);
    }

    public override string? GetTypeKey() => "creature";

    public override Message? GetStatusString()
    {
        var view = GetView();
        if (view == null) return null;

        var parts = new List<Message>
        {
            Message.Localized("ui", "RESOURCE.HP", new { current = view.CurrentHp, max = view.MaxHp }),
        };

        if (view.Block > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = view.Block }));

        var intentSummary = CreatureIntentFormatter.Summary(view, includePrefix: true);
        if (intentSummary is { IsEmpty: false })
            parts.Add(intentSummary);

        return Message.Join(", ", parts.ToArray());
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var view = GetView();
        if (view == null) return base.HandleBuffers(buffers);

        // While aiming a card at this target, expose the card's text with its
        // numbers updated for this creature (Vulnerable, etc.) in the card
        // buffer alongside — matching the preview the sighted UI shows. The
        // current buffer still stays on the creature/player buffer below; the
        // card buffer just sits next to it for review. Re-evaluated every focus,
        // and ResetToAlwaysEnabled drops it again once you're no longer aiming.
        var playedCard = Screens.CombatScreen.CurrentPlayedCard();
        if (playedCard != null && buffers.GetBuffer("card") is CardBuffer cardBuffer)
        {
            cardBuffer.Bind(playedCard, view.Entity);
            cardBuffer.Update();
            buffers.EnableBuffer("card", true);
        }

        // Local player: use the player buffer, bound to null
        if (view.IsLocalPlayer)
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

        // Another player in multiplayer: bind the player buffer to them
        if (view.IsPlayer && view.Player != null)
        {
            var playerBuffer = buffers.GetBuffer("player") as PlayerBuffer;
            if (playerBuffer != null)
            {
                playerBuffer.Bind(view.Player);
                playerBuffer.Update();
                buffers.EnableBuffer("player", true);
            }
            return "player";
        }

        var creatureBuffer = buffers.GetBuffer("creature") as CreatureBuffer;
        if (creatureBuffer != null)
        {
            creatureBuffer.Bind(view.Entity);
            creatureBuffer.Update();
            buffers.EnableBuffer("creature", true);
        }
        return "creature";
    }
}
