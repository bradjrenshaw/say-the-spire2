using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("map_vote", "Map Vote", hasSourceFilter: true, allowEnemies: false)]
public class MapVoteEvent : GameEvent
{
    public enum VoteKind
    {
        RemoteVote,
        LocalVote,
        Travel,
        RemoteSkip,
        LocalSkip,
    }

    private readonly string _playerName;
    private readonly string _targetName;
    private readonly VoteKind _kind;

    public MapVoteEvent(string playerName, string targetName, Creature? source = null,
        VoteKind kind = VoteKind.RemoteVote)
    {
        Source = source;
        _playerName = playerName;
        _targetName = targetName;
        _kind = kind;
    }

    public override Message? GetMessage() => _kind switch
    {
        VoteKind.RemoteVote => Message.Localized("ui", "EVENT.MAP_VOTE", new
        {
            player = _playerName,
            node = _targetName
        }),
        VoteKind.LocalVote => Message.Localized("ui", "EVENT.MAP_VOTE_LOCAL", new
        {
            node = _targetName
        }),
        VoteKind.Travel => Message.Localized("ui", "EVENT.MAP_TRAVEL", new
        {
            node = _targetName
        }),
        VoteKind.RemoteSkip => Message.Localized("ui", "EVENT.MAP_VOTE_SKIP", new
        {
            player = _playerName
        }),
        VoteKind.LocalSkip => Message.Localized("ui", "EVENT.MAP_VOTE_SKIP_LOCAL"),
        _ => Message.Localized("ui", "EVENT.MAP_VOTE", new
        {
            player = _playerName,
            node = _targetName
        })
    };

    public override bool ShouldAddToBuffer() => false;
}
