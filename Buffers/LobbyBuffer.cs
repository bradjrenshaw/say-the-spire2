using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.Buffers;

[BufferAnnouncementOrder(
    typeof(LobbyPlayersAnnouncement)
)]
public class LobbyBuffer : Buffer
{
    private StartRunLobby? _lobby;

    public LobbyBuffer() : base("lobby") { }

    public void Bind(StartRunLobby lobby)
    {
        _lobby = lobby;
    }

    protected override void ClearBinding()
    {
        _lobby = null;
        Clear();
    }

    public override void Update()
    {
        if (_lobby == null) return;
        Repopulate(() => Populate(this, _lobby));
    }

    public static void Populate(Buffer buffer, StartRunLobby? lobby)
    {
        var attrOrder = typeof(LobbyBuffer).GetCustomAttributes(typeof(BufferAnnouncementOrderAttribute), inherit: true)
            is { Length: > 0 } attrs && attrs[0] is BufferAnnouncementOrderAttribute order
            ? order.Types
            : Array.Empty<Type>();

        BufferAnnouncementComposer.Compose(buffer, "lobby", attrOrder, BuildAnnouncements(lobby));
    }

    private static IEnumerable<Announcement> BuildAnnouncements(StartRunLobby? lobby)
    {
        yield return new LobbyPlayersAnnouncement(lobby);
    }
}
