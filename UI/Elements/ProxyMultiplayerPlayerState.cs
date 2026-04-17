using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(ControlValueAnnouncement)
)]
public class ProxyMultiplayerPlayerState : ProxyElement
{
    private readonly NMultiplayerPlayerState _state;

    public ProxyMultiplayerPlayerState(Control hitbox, NMultiplayerPlayerState state)
        : base(hitbox)
    {
        _state = state;
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var status = GetStatusString();
        if (status != null)
            yield return new ControlValueAnnouncement(status);
    }

    private Player? GetPlayer()
    {
        try { return _state.Player; }
        catch (System.Exception e) { MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] MultiplayerPlayerState.Player access failed: {e.Message}"); return null; }
    }

    public override Message? GetLabel()
    {
        var player = GetPlayer();
        if (player == null) return Message.Localized("ui", "LABELS.PLAYER");

        var name = MultiplayerHelper.GetPlayerName(player.NetId);
        var character = player.Character?.Title?.GetFormattedText();
        return !string.IsNullOrEmpty(character) ? Message.Raw($"{name}, {character}") : Message.Raw(name);
    }

    public override string? GetTypeKey() => null;

    public override Message? GetStatusString()
    {
        var player = GetPlayer();
        if (player == null) return null;

        var parts = new System.Collections.Generic.List<string>();
        var creature = player.Creature;

        parts.Add(Message.Localized("ui", "RESOURCE.HP", new { current = creature.CurrentHp, max = creature.MaxHp }).Resolve());

        if (creature.Block > 0)
            parts.Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = creature.Block }).Resolve());

        var pcs = player.PlayerCombatState;
        if (pcs != null)
        {
            parts.Add(ResourceHelper.GetResourceString(pcs));
            parts.Add(Message.Localized("ui", "RESOURCE.CARDS_IN_HAND", new { count = pcs.Hand.Cards.Count }).Resolve());
        }

        return Message.Raw(string.Join(", ", parts));
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
