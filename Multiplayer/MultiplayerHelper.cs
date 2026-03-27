using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace SayTheSpire2.Multiplayer;

/// <summary>
/// Shared multiplayer utilities. Consolidates player name resolution,
/// multiplayer detection, and local player checks used across hooks,
/// screens, and buffers.
/// </summary>
public static class MultiplayerHelper
{
    /// <summary>
    /// Get a player's display name from their network ID.
    /// Uses RunManager's NetService during gameplay, or an explicit platform if provided.
    /// Falls back to "Player {netId}" on failure.
    /// </summary>
    public static string GetPlayerName(ulong netId, PlatformType? platform = null)
    {
        try
        {
            var p = platform ?? RunManager.Instance.NetService.Platform;
            return PlatformUtil.GetPlayerName(p, netId);
        }
        catch { }
        return $"Player {netId}";
    }

    /// <summary>
    /// Get a player's display name from a Player object (in-game).
    /// </summary>
    public static string GetPlayerName(Player player)
    {
        return GetPlayerName(player.NetId);
    }

    /// <summary>
    /// Get the spoken/display name for a creature.
    /// In multiplayer, player creatures now expose NetId through Creature.Name,
    /// so resolve through PlatformUtil instead.
    /// </summary>
    public static string GetCreatureName(Creature creature, PlatformType? platform = null)
    {
        try
        {
            if (creature.IsPlayer && creature.Player != null && !RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
                return GetPlayerName(creature.Player.NetId, platform);
        }
        catch { }

        return creature.Name;
    }

    /// <summary>
    /// Check if the current run is multiplayer.
    /// Returns false outside of a run or on error.
    /// </summary>
    public static bool IsMultiplayer()
    {
        try { return RunManager.Instance.NetService.Type.IsMultiplayer(); }
        catch { return false; }
    }

    /// <summary>
    /// Check if a Player is the local player.
    /// </summary>
    public static bool IsLocalPlayer(Player player)
    {
        return LocalContext.NetId.HasValue && player.NetId == LocalContext.NetId.Value;
    }
}
