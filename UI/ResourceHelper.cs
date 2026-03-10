using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;

namespace SayTheSpire2.UI;

/// <summary>
/// Builds resource strings from combat state. Centralizes formatting so
/// energy, stars, and any future resources are consistent everywhere.
/// </summary>
public static class ResourceHelper
{
    public static string GetResourceString(PlayerCombatState pcs)
    {
        var parts = new List<string>();
        parts.Add($"{pcs.Energy}/{pcs.MaxEnergy} energy");
        if (pcs.Stars > 0)
            parts.Add($"{pcs.Stars} stars");
        return string.Join(", ", parts);
    }
}
