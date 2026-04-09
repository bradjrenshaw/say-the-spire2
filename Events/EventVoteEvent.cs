using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("event_vote", "Event Vote", hasSourceFilter: true, allowEnemies: false)]
public class EventVoteEvent : GameEvent
{
    private readonly string _playerName;
    private readonly string _optionText;

    public EventVoteEvent(string playerName, string optionText, Creature? source = null)
    {
        Source = source;
        _playerName = playerName;
        _optionText = optionText;
    }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.EVENT_VOTE", new { player = _playerName, option = _optionText });
    public override bool ShouldAddToBuffer() => false;
}
