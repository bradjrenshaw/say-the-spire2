using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("map_vote", "Map Vote", hasSourceFilter: true, allowEnemies: false)]
public class MapVoteEvent : GameEvent
{
    private readonly string _playerName;
    private readonly string _nodeName;

    public MapVoteEvent(string playerName, string nodeName, Creature? source = null)
    {
        Source = source;
        _playerName = playerName;
        _nodeName = nodeName;
    }

    public override Message? GetMessage() => Message.Localized("ui", "EVENT.MAP_VOTE", new { player = _playerName, node = _nodeName });
    public override bool ShouldAddToBuffer() => false;
}
