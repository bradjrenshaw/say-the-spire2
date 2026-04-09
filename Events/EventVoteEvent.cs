using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("event_vote", "Event Vote", hasSourceFilter: true, allowEnemies: false)]
public class EventVoteEvent : GameEvent
{
    private readonly string _message;

    public EventVoteEvent(string message, Creature? source = null)
    {
        Source = source;
        _message = message;
    }

    public override Message? GetMessage() => Message.Raw(_message);
    public override bool ShouldAddToBuffer() => false;
}
