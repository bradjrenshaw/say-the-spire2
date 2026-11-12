using MegaCrit.Sts2.Core.Entities.Creatures;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("map_vote", "Map Vote", category: "Multiplayer", hasSourceFilter: true, allowEnemies: false)]
public class MapVoteEvent : GameEvent
{
    public enum VoteKind
    {
        RemoteVote,
        LocalVote,
        RemoteSkip,
        LocalSkip,
        // Blitz has no map to vote on: pressing Proceed is a vote for the
        // one next room, so it reads as being ready to move on.
        RemoteProceed,
        LocalProceed,
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
        VoteKind.RemoteSkip => Message.Localized("ui", "EVENT.MAP_VOTE_SKIP", new
        {
            player = _playerName
        }),
        VoteKind.LocalSkip => Message.Localized("ui", "EVENT.MAP_VOTE_SKIP_LOCAL"),
        VoteKind.RemoteProceed => Message.Localized("ui", "SPEECH.PLAYER_READY_PROCEED", new
        {
            name = _playerName
        }),
        VoteKind.LocalProceed => Message.Localized("ui", "SPEECH.MARKED_READY"),
        _ => Message.Localized("ui", "EVENT.MAP_VOTE", new
        {
            player = _playerName,
            node = _targetName
        })
    };

    public override bool ShouldAddToBuffer() => false;
}
