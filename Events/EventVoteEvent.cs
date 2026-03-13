using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("event_vote", "Event Vote")]
public class EventVoteEvent : GameEvent
{
    private readonly string _message;

    public EventVoteEvent(string message)
    {
        _message = message;
    }

    public override string? GetMessage() => _message;
    public override bool ShouldAddToBuffer() => false;
}
