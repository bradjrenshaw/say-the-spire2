using System.Collections.Generic;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Single-group buffer announcement that yields one line per player in a
/// lobby ("Name, Character, Ready"). Dynamic membership — users can move
/// the group as a whole in the buffer order but can't reorder individual
/// players (they come and go each frame).
/// </summary>
public sealed class LobbyPlayersAnnouncement : Announcement
{
    private readonly StartRunLobby? _lobby;

    public LobbyPlayersAnnouncement(StartRunLobby? lobby) { _lobby = lobby; }

    public override string Key => "lobby_players";

    public override Message Render(AnnouncementContext ctx) => Message.Empty;

    public override IEnumerable<Message> RenderBuffer(AnnouncementContext ctx)
    {
        if (_lobby == null) yield break;

        foreach (var player in _lobby.Players)
        {
            var name = MultiplayerHelper.GetPlayerName(player.id, _lobby.NetService.Platform);
            var character = player.character?.Title?.GetFormattedText()
                ?? LocalizationManager.GetOrDefault("ui", "DAILY_RUN.NO_CHARACTER", "No character");
            var ready = player.isReady
                ? LocalizationManager.GetOrDefault("ui", "DAILY_RUN.READY", "Ready")
                : LocalizationManager.GetOrDefault("ui", "DAILY_RUN.NOT_READY", "Not ready");
            yield return Message.Raw($"{name}, {character}, {ready}");
        }
    }
}
