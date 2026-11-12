using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
using SayTheSpire2.Map;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// A point on the Blitz top-bar map (<c>NBlitzTopBarMapPoint</c>, November
/// beta). Blitz replaces the map screen and the room/floor/boss icons with
/// this strip: one point per room of the act, in order. Reads the room type,
/// whether it is behind us or where we are now, its place in the act, and
/// the game's own hover tip for the room type.
///
/// For rooms already visited the game shows that room's history instead of
/// the room-type tip; that history isn't read here, only "traveled".
///
/// The control type is beta-only, so it is resolved by name.
/// </summary>
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(NodeStateAnnouncement),
    typeof(PositionAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyBlitzMapPoint : ProxyElement
{
    private static readonly Type? ControlType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.CommonUi.NBlitzTopBarMapPoint");

    // Resolved only where the type exists; a rename there should crash.
    private static readonly PropertyInfo? PointProp =
        ControlType == null ? null : AccessTools.Property(ControlType, "Point")!;
    private static readonly FieldInfo? RunStateField =
        ControlType == null ? null : AccessTools.Field(ControlType, "_runState")!;

    public static bool Handles(Control control) =>
        ControlType != null && ControlType.IsInstanceOfType(control);

    public ProxyBlitzMapPoint(Control control) : base(control) { }

    private MapPoint? Point => PointProp?.GetValue(Control) as MapPoint;
    private IRunState? RunState => RunStateField?.GetValue(Control) as IRunState;

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var state = GetStatusString();
        if (state != null)
            yield return new NodeStateAnnouncement(state.Resolve());

        var position = GetPosition();
        if (position != null)
            yield return new PositionAnnouncement(position);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        var point = Point;
        if (point == null)
            return null;

        // As the game's tip does, name the act's boss on the boss point.
        if (point.PointType == MapPointType.Boss && RunState is { } runState)
            return Message.Localized("ui", "TOPBAR.BOSS",
                new { name = runState.Act.BossEncounter.Title.GetFormattedText() });

        return Message.Raw(MapNode.GetPointTypeName(point.PointType));
    }

    /// <summary>"current location" for the room we're in, "traveled" for the ones behind us.</summary>
    public override Message? GetStatusString()
    {
        var point = Point;
        var current = RunState?.CurrentMapPoint;
        if (point == null || current == null)
            return null;

        if (point.coord == current.coord)
            return Message.Localized("map_nav", "STATE.CURRENT");
        return point.coord.row < current.coord.row
            ? Message.Localized("map_nav", "STATE.TRAVELED")
            : null;
    }

    // The act is a single line, one room per row, ending at the (last) boss.
    private Message? GetPosition()
    {
        var point = Point;
        var map = RunState?.Map;
        if (point == null || map == null)
            return null;

        var last = map.SecondBossMapPoint ?? map.BossMapPoint;
        return Message.Localized("ui", "POSITIONS.LIST",
            new { position = point.coord.row + 1, total = last.coord.row + 1 });
    }

    /// <summary>
    /// The game's room-type hover tip. Not shown for rooms already visited,
    /// where the game shows that room's history instead.
    /// </summary>
    public override Message? GetTooltip()
    {
        var point = Point;
        if (point == null || GetLegendKey(point.PointType) is not { } legendKey)
            return null;

        var current = RunState?.CurrentMapPoint;
        if (current != null && point.coord.row < current.coord.row)
            return null;

        var description = new LocString("map", legendKey + ".hoverTip.description").GetFormattedText();
        return string.IsNullOrEmpty(description) ? null : Message.Raw(StripBbcode(description));
    }

    private static string? GetLegendKey(MapPointType pointType) => pointType switch
    {
        MapPointType.Unknown => "LEGEND_UNKNOWN",
        MapPointType.Shop => "LEGEND_MERCHANT",
        MapPointType.Treasure => "LEGEND_TREASURE",
        MapPointType.RestSite => "LEGEND_REST",
        MapPointType.Monster => "LEGEND_ENEMY",
        MapPointType.Elite => "LEGEND_ELITE",
        MapPointType.Ancient => "LEGEND_ANCIENT",
        MapPointType.Boss => "LEGEND_BOSS",
        _ => null,
    };
}
