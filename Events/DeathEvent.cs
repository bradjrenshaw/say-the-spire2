using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("death", "Death", hasSourceFilter: true)]
public class DeathEvent : GameEvent
{
    private readonly string _creatureName;

    public DeathEvent(Creature creature)
    {
        Source = creature;
        _creatureName = MultiplayerHelper.GetCreatureName(creature);
    }

    public override Message? GetMessage() => Message.Raw($"{_creatureName} died");
}
