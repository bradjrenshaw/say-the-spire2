using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Multiplayer;

namespace SayTheSpire2.UI.Elements;

public class ProxyMultiplayerPlayerState : ProxyElement
{
    private readonly NMultiplayerPlayerState _state;

    public ProxyMultiplayerPlayerState(Control hitbox, NMultiplayerPlayerState state)
        : base(hitbox)
    {
        _state = state;
    }

    private Player? GetPlayer()
    {
        try { return _state.Player; }
        catch { return null; }
    }

    public override string? GetLabel()
    {
        var player = GetPlayer();
        if (player == null) return "Player";

        var name = MultiplayerHelper.GetPlayerName(player.NetId);
        var character = player.Character?.Title?.GetFormattedText();
        return !string.IsNullOrEmpty(character) ? $"{name}, {character}" : name;
    }

    public override string? GetTypeKey() => null;

    public override string? GetStatusString()
    {
        var player = GetPlayer();
        if (player == null) return null;

        var parts = new System.Collections.Generic.List<string>();
        var creature = player.Creature;

        parts.Add($"{creature.CurrentHp}/{creature.MaxHp} HP");

        if (creature.Block > 0)
            parts.Add($"{creature.Block} Block");

        var pcs = player.PlayerCombatState;
        if (pcs != null)
        {
            parts.Add($"{pcs.Energy} energy");
            if (pcs.Stars > 0)
                parts.Add($"{pcs.Stars} stars");
            parts.Add($"{pcs.Hand.Cards.Count} cards in hand");
        }

        return string.Join(", ", parts);
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var player = GetPlayer();
        if (player == null) return base.HandleBuffers(buffers);

        var playerBuffer = buffers.GetBuffer("player") as PlayerBuffer;
        if (playerBuffer != null)
        {
            playerBuffer.Bind(player);
            playerBuffer.Update();
            buffers.EnableBuffer("player", true);
        }

        return "player";
    }
}
