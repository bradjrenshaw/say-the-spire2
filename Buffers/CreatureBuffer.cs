using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;
namespace SayTheSpire2.Buffers;

[BufferAnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(HpAnnouncement),
    typeof(BlockAnnouncement),
    typeof(MonsterIntentsAnnouncement),
    typeof(PowersAnnouncement)
)]
public class CreatureBuffer : Buffer
{
    private Creature? _creature;

    public CreatureBuffer() : base("creature") { }

    public void Bind(Creature creature)
    {
        _creature = creature;
    }

    protected override void ClearBinding()
    {
        _creature = null;
        Clear();
    }

    public override void Update()
    {
        if (_creature == null) return;
        Repopulate(() => Populate(this, _creature));
    }

    public static void Populate(Buffer buffer, Creature creature)
    {
        var attrOrder = typeof(CreatureBuffer).GetCustomAttributes(typeof(BufferAnnouncementOrderAttribute), inherit: true)
            is { Length: > 0 } attrs && attrs[0] is BufferAnnouncementOrderAttribute order
            ? order.Types
            : Array.Empty<Type>();

        BufferAnnouncementComposer.Compose(buffer, "creature", attrOrder, BuildAnnouncements(creature));
    }

    private static IEnumerable<Announcement> BuildAnnouncements(Creature creature)
    {
        yield return new LabelAnnouncement(MultiplayerHelper.GetCreatureName(creature));
        yield return new HpAnnouncement(creature.CurrentHp, creature.MaxHp);

        if (creature.Block > 0)
            yield return new BlockAnnouncement(creature.Block);

        IReadOnlyList<IntentView> intents = Array.Empty<IntentView>();
        if (creature.IsMonster && creature.Monster != null)
        {
            try
            {
                var rawIntents = creature.Monster.NextMove.Intents;
                if (rawIntents != null && rawIntents.Count > 0)
                {
                    var allies = CreatureView.GetCombatStateAllies(creature);
                    intents = rawIntents
                        .Select(i => IntentView.FromIntent(i, creature, allies))
                        .ToList();
                }
            }
            catch
            {
                // Intent access may fail outside combat — leave intents empty.
            }
        }
        yield return new MonsterIntentsAnnouncement(intents);

        if (creature.Powers.Count > 0)
            yield return new PowersAnnouncement(creature.Powers);
    }
}
