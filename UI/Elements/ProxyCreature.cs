using System.Collections.Generic;
using Godot;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.UI;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
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
                if (!string.IsNullOrEmpty(summary))
                    yield return new PlayerIntentsAnnouncement(summary);
            }
        }
    }

    public ProxyCreature(Control control) : base(control) { }

    private CreatureView? GetView() => CreatureView.FromControl(Control);

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

        var parts = new List<string>
        {
            Message.Localized("ui", "RESOURCE.HP", new { current = view.CurrentHp, max = view.MaxHp }).Resolve(),
        };

        if (view.Block > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = view.Block }).Resolve());

        var intentSummary = CreatureIntentFormatter.Summary(view, includePrefix: true);
        if (!string.IsNullOrEmpty(intentSummary))
            parts.Add(intentSummary);

        return Message.Raw(string.Join(", ", parts));
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var view = GetView();
        if (view == null) return base.HandleBuffers(buffers);

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
