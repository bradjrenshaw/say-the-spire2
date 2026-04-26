using System.Collections.Generic;
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
        catch (System.Exception e) { MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] GetPlayerName failed for {netId}: {e.Message}"); }
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
    /// Resolve display names for a sequence of players, skipping any that fail.
    /// Returns an empty list when the input is null or empty so callers can
    /// safely chain Count / string.Join.
    /// </summary>
    public static List<string> GetPlayerNames(IEnumerable<Player>? players, PlatformType? platform = null)
    {
        var names = new List<string>();
        if (players == null) return names;

        foreach (var player in players)
        {
            try { names.Add(GetPlayerName(player.NetId, platform)); }
            catch (System.Exception e)
            {
                MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] GetPlayerNames failed for {player.NetId}: {e.Message}");
            }
        }
        return names;
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
        catch (System.Exception e) { MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] GetCreatureName multiplayer check failed: {e.Message}"); }

        return creature.Name;
    }

    /// <summary>
    /// Get the best display name for a player: creature name if in combat, otherwise network name.
    /// </summary>
    public static string GetPlayerDisplayName(Player player, PlatformType? platform = null)
    {
        return player.Creature != null
            ? GetCreatureName(player.Creature, platform)
            : GetPlayerName(player.NetId, platform);
    }

    /// <summary>
    /// Check if the current run is multiplayer.
    /// Returns false outside of a run or on error.
    /// </summary>
    public static bool IsMultiplayer()
    {
        try { return RunManager.Instance.NetService.Type.IsMultiplayer(); }
        catch (System.Exception e) { MegaCrit.Sts2.Core.Logging.Log.Info($"[AccessibilityMod] IsMultiplayer check failed: {e.Message}"); return false; }
    }

    /// <summary>
    /// Check if a Player is the local player.
    /// </summary>
    public static bool IsLocalPlayer(Player player)
    {
        return LocalContext.NetId.HasValue && player.NetId == LocalContext.NetId.Value;
    }
}
