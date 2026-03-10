using System.Reflection;
using Godot;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyTopBar : ProxyElement
{
    private enum TopBarType { Hp, Gold, Room, Floor, Boss }

    private static readonly FieldInfo? HpPlayerField =
        typeof(NTopBarHp).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? GoldPlayerField =
        typeof(NTopBarGold).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? RoomRunStateField =
        typeof(NTopBarRoomIcon).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? FloorRunStateField =
        typeof(NTopBarFloorIcon).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? BossRunStateField =
        typeof(NTopBarBossIcon).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly TopBarType _type;

    public ProxyTopBar(Control control) : base(control)
    {
        _type = control switch
        {
            NTopBarHp => TopBarType.Hp,
            NTopBarGold => TopBarType.Gold,
            NTopBarRoomIcon => TopBarType.Room,
            NTopBarFloorIcon => TopBarType.Floor,
            NTopBarBossIcon => TopBarType.Boss,
            _ => TopBarType.Hp
        };
    }

    public override string? GetLabel()
    {
        try
        {
            return _type switch
            {
                TopBarType.Hp => GetHpLabel(),
                TopBarType.Gold => GetGoldLabel(),
                TopBarType.Room => GetRoomLabel(),
                TopBarType.Floor => GetFloorLabel(),
                TopBarType.Boss => GetBossLabel(),
                _ => CleanNodeName(Control.Name)
            };
        }
        catch
        {
            return CleanNodeName(Control.Name);
        }
    }

    public override string? GetTooltip()
    {
        try
        {
            var (_, tipDesc) = GetFormattedTooltip();
            return !string.IsNullOrEmpty(tipDesc) ? StripBbcode(tipDesc) : null;
        }
        catch
        {
            return null;
        }
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null) return base.HandleBuffers(buffers);

        uiBuffer.Clear();

        var label = GetLabel();
        if (!string.IsNullOrEmpty(label))
            uiBuffer.Add(label);

        try
        {
            var (tipTitle, tipDesc) = GetFormattedTooltip();
            if (!string.IsNullOrEmpty(tipTitle))
                uiBuffer.Add(StripBbcode(tipTitle));
            if (!string.IsNullOrEmpty(tipDesc))
                uiBuffer.Add(StripBbcode(tipDesc));
        }
        catch { }

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    private string? GetHpLabel()
    {
        var player = HpPlayerField?.GetValue(Control) as MegaCrit.Sts2.Core.Entities.Players.Player;
        if (player == null) return "HP";
        return $"HP {player.Creature.CurrentHp}/{player.Creature.MaxHp}";
    }

    private string? GetGoldLabel()
    {
        var player = GoldPlayerField?.GetValue(Control) as MegaCrit.Sts2.Core.Entities.Players.Player;
        if (player == null) return "Gold";
        return $"Gold {player.Gold}";
    }

    private string? GetRoomLabel()
    {
        var runState = RoomRunStateField?.GetValue(Control) as IRunState;
        if (runState == null) return "Room";
        var tipKey = GetRoomTipPrefix(runState);
        var title = new LocString("static_hover_tips", tipKey + ".title").GetFormattedText();
        return !string.IsNullOrEmpty(title) ? StripBbcode(title) : "Room";
    }

    private string? GetFloorLabel()
    {
        var runState = FloorRunStateField?.GetValue(Control) as IRunState;
        if (runState == null) return "Floor";
        return $"Floor {runState.TotalFloor}";
    }

    private string? GetBossLabel()
    {
        var runState = BossRunStateField?.GetValue(Control) as IRunState;
        if (runState == null) return "Boss";

        var boss1 = runState.Act.BossEncounter;
        var boss2 = runState.Act.SecondBossEncounter;

        if (boss2 != null && !ShouldOnlyShowSecondBoss(runState))
            return $"Boss {boss1.Title.GetFormattedText()} and {boss2.Title.GetFormattedText()}";

        var activeBoss = (boss2 != null && ShouldOnlyShowSecondBoss(runState)) ? boss2 : boss1;
        return $"Boss {activeBoss.Title.GetFormattedText()}";
    }

    private (string? title, string? desc) GetFormattedTooltip()
    {
        if (_type == TopBarType.Boss)
            return GetBossTooltip();

        var tipKey = GetHoverTipKey();
        if (tipKey == null) return (null, null);

        var title = new LocString("static_hover_tips", tipKey + ".title").GetFormattedText();
        var desc = new LocString("static_hover_tips", tipKey + ".description").GetFormattedText();
        return (title, desc);
    }

    private (string? title, string? desc) GetBossTooltip()
    {
        try
        {
            var runState = BossRunStateField?.GetValue(Control) as IRunState;
            if (runState == null) return (null, null);

            var boss1 = runState.Act.BossEncounter;
            var boss2 = runState.Act.SecondBossEncounter;

            if (boss2 != null && !ShouldOnlyShowSecondBoss(runState))
            {
                var titleLoc = new LocString("static_hover_tips", "DOUBLE_BOSS.title");
                titleLoc.Add("BossName1", boss1.Title);
                titleLoc.Add("BossName2", boss2.Title);
                var descLoc = new LocString("static_hover_tips", "DOUBLE_BOSS.description");
                descLoc.Add("BossName1", boss1.Title);
                descLoc.Add("BossName2", boss2.Title);
                return (titleLoc.GetFormattedText(), descLoc.GetFormattedText());
            }

            var activeBoss = (boss2 != null && ShouldOnlyShowSecondBoss(runState)) ? boss2 : boss1;
            var singleTitle = new LocString("static_hover_tips", "BOSS.title");
            singleTitle.Add("BossName", activeBoss.Title);
            var singleDesc = new LocString("static_hover_tips", "BOSS.description");
            singleDesc.Add("BossName", activeBoss.Title);
            return (singleTitle.GetFormattedText(), singleDesc.GetFormattedText());
        }
        catch { return (null, null); }
    }

    private string? GetHoverTipKey()
    {
        return _type switch
        {
            TopBarType.Hp => "HIT_POINTS",
            TopBarType.Gold => "MONEY_POUCH",
            TopBarType.Floor => "FLOOR",
            TopBarType.Room => GetRoomTipPrefixSafe(),
            TopBarType.Boss => GetBossTipPrefixSafe(),
            _ => null
        };
    }

    private string? GetRoomTipPrefixSafe()
    {
        try
        {
            var runState = RoomRunStateField?.GetValue(Control) as IRunState;
            if (runState == null) return null;
            return GetRoomTipPrefix(runState);
        }
        catch { return null; }
    }

    private static string GetRoomTipPrefix(IRunState runState)
    {
        var pointType = runState.CurrentMapPoint?.PointType ?? MapPointType.Unassigned;
        return pointType switch
        {
            MapPointType.Unassigned => "ROOM_MAP",
            MapPointType.Unknown => GetUnknownRoomPrefix(runState),
            MapPointType.Shop => "ROOM_MERCHANT",
            MapPointType.Treasure => "ROOM_TREASURE",
            MapPointType.RestSite => "ROOM_REST",
            MapPointType.Monster => "ROOM_ENEMY",
            MapPointType.Elite => "ROOM_ELITE",
            MapPointType.Boss => "ROOM_BOSS",
            MapPointType.Ancient => "ROOM_ANCIENT",
            _ => "ROOM_MAP"
        };
    }

    private static string GetUnknownRoomPrefix(IRunState runState)
    {
        var roomType = runState.BaseRoom?.RoomType;
        return roomType switch
        {
            RoomType.Monster => "ROOM_UNKNOWN_ENEMY",
            RoomType.Treasure => "ROOM_UNKNOWN_TREASURE",
            RoomType.Shop => "ROOM_UNKNOWN_MERCHANT",
            RoomType.Event => "ROOM_UNKNOWN_EVENT",
            _ => "ROOM_MAP"
        };
    }

    private string? GetBossTipPrefixSafe()
    {
        try
        {
            var runState = BossRunStateField?.GetValue(Control) as IRunState;
            if (runState == null) return null;
            var boss2 = runState.Act.SecondBossEncounter;
            if (boss2 != null && !ShouldOnlyShowSecondBoss(runState))
                return "DOUBLE_BOSS";
            return "BOSS";
        }
        catch { return null; }
    }

    private static bool ShouldOnlyShowSecondBoss(IRunState runState)
    {
        return runState.Map.SecondBossMapPoint != null
            && runState.CurrentMapPoint == runState.Map.BossMapPoint;
    }
}
