using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("map_vote", "Map Vote", hasSourceFilter: true, allowEnemies: false)]
public class MapVoteEvent : GameEvent
{
    private readonly string _message;

    public MapVoteEvent(string message, Creature? source = null)
    {
        Source = source;
        _message = message;
    }

    public override Message? GetMessage() => Message.Raw(_message);
    public override bool ShouldAddToBuffer() => false;
}
