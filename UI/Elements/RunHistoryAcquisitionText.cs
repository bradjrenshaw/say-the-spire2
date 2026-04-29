using System.Collections.Generic;
using System.Linq;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

internal static class RunHistoryAcquisitionText
{
    public static string? FromFloor(int floor)
    {
        return floor > 0
            ? Message.Localized("ui", "RUN_HISTORY.OBTAINED_ON_FLOOR", new { floor }).Resolve()
            : null;
    }

    public static string? FromFloors(IEnumerable<int>? floors)
    {
        var uniqueFloors = floors?
            .Where(floor => floor > 0)
            .Distinct()
            .OrderBy(floor => floor)
            .ToArray();

        if (uniqueFloors == null || uniqueFloors.Length == 0)
            return null;

        return uniqueFloors.Length == 1
            ? FromFloor(uniqueFloors[0])
            : Message.Localized("ui", "RUN_HISTORY.OBTAINED_ON_FLOORS", new { floors = string.Join(", ", uniqueFloors) }).Resolve();
    }
}
