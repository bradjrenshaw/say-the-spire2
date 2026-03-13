using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("map_vote", "Map Vote")]
public class MapVoteEvent : GameEvent
{
    private readonly string _message;

    public MapVoteEvent(string message)
    {
        _message = message;
    }

    public override string? GetMessage() => _message;
    public override bool ShouldAddToBuffer() => false;
}
