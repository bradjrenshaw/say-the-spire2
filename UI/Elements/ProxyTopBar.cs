using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

public class ProxyTopBar : ProxyElement
{
    // User-perceives this as a button; share settings / [AnnouncementOrder] with ProxyButton.
    public override System.Type AnnouncementOrderType => typeof(ProxyButton);

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    private enum TopBarType { Hp, Gold, Room, Floor, Boss }

    private static readonly FieldInfo? HpPlayerField =
        AccessTools.Field(typeof(NTopBarHp), "_player");
    private static readonly FieldInfo? GoldPlayerField =
        AccessTools.Field(typeof(NTopBarGold), "_player");
    private static readonly FieldInfo? RoomRunStateField =
        AccessTools.Field(typeof(NTopBarRoomIcon), "_runState");
    private static readonly FieldInfo? FloorRunStateField =
        AccessTools.Field(typeof(NTopBarFloorIcon), "_runState");
    private static readonly FieldInfo? BossRunStateField =
        AccessTools.Field(typeof(NTopBarBossIcon), "_runState");

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

    public override SayTheSpire2.Localization.Message? GetLabel()
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
                _ => Control != null ? SayTheSpire2.Localization.Message.Raw(CleanNodeName(Control.Name)) : null
            };
        }
        catch
        {
            return Control != null ? SayTheSpire2.Localization.Message.Raw(CleanNodeName(Control.Name)) : null;
        }
    }

    public override SayTheSpire2.Localization.Message? GetTooltip()
    {
        try
        {
            var (_, tipDesc) = GetFormattedTooltip();
            var plain = !string.IsNullOrEmpty(tipDesc) ? StripBbcode(tipDesc) : null;

            // Dread: the boss icon's tip is the act's Dominion, whose title
            // ("Test Subject's Dominion") is what says the rule is one. In
            // the last act it follows the two-boss tip.
            if (_type == TopBarType.Boss && GetDominion() is { } dominion)
            {
                var rule = $"{dominion.DominionTitle}: {dominion.DominionDescription}";
                return SayTheSpire2.Localization.Message.Raw(plain != null ? $"{plain} {rule}" : rule);
            }

            return plain != null ? SayTheSpire2.Localization.Message.Raw(plain) : null;
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

        var label = GetLabel()?.Resolve();
        if (!string.IsNullOrEmpty(label))
            uiBuffer.Add(label);

        try
        {
            var (tipTitle, tipDesc) = GetFormattedTooltip();
            if (!string.IsNullOrEmpty(tipTitle))
                uiBuffer.Add(StripBbcode(tipTitle));
            if (!string.IsNullOrEmpty(tipDesc))
                uiBuffer.Add(StripBbcode(tipDesc));

            // Dread: the Dominion tip, then its own extra tips (cards and
            // keywords it names).
            if (_type == TopBarType.Boss && GetDominion() is { } dominion)
            {
                uiBuffer.Add(dominion.DominionTitle);
                if (dominion.DominionDescription is { Length: > 0 } rule)
                    uiBuffer.Add(rule);
                foreach (var line in dominion.DominionExtraTips)
                    uiBuffer.Add(line);
            }
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Top bar tooltip access failed: {e.Message}"); }

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    private SayTheSpire2.Localization.Message GetHpLabel()
    {
        var player = HpPlayerField?.GetValue(Control) as MegaCrit.Sts2.Core.Entities.Players.Player;
        if (player == null) return SayTheSpire2.Localization.Message.Localized("ui", "LABELS.HP");
        return SayTheSpire2.Localization.Message.Localized("ui", "TOPBAR.HP", new { current = player.Creature.CurrentHp, max = player.Creature.MaxHp });
    }

    private SayTheSpire2.Localization.Message GetGoldLabel()
    {
        var player = GoldPlayerField?.GetValue(Control) as MegaCrit.Sts2.Core.Entities.Players.Player;
        if (player == null) return SayTheSpire2.Localization.Message.Localized("ui", "LABELS.GOLD");
        return SayTheSpire2.Localization.Message.Localized("ui", "TOPBAR.GOLD", new { amount = player.Gold });
    }

    private SayTheSpire2.Localization.Message GetRoomLabel()
    {
        var runState = RoomRunStateField?.GetValue(Control) as IRunState;
        if (runState == null) return SayTheSpire2.Localization.Message.Localized("ui", "LABELS.ROOM");
        var tipKey = GetRoomTipPrefix(runState);
        var title = new LocString("static_hover_tips", tipKey + ".title").GetFormattedText();
        return !string.IsNullOrEmpty(title)
            ? SayTheSpire2.Localization.Message.Raw(StripBbcode(title))
            : SayTheSpire2.Localization.Message.Localized("ui", "LABELS.ROOM");
    }

    private SayTheSpire2.Localization.Message GetFloorLabel()
    {
        var runState = FloorRunStateField?.GetValue(Control) as IRunState;
        if (runState == null) return SayTheSpire2.Localization.Message.Localized("ui", "LABELS.FLOOR");
        return SayTheSpire2.Localization.Message.Localized("ui", "TOPBAR.FLOOR", new { floor = runState.TotalFloor });
    }

    private SayTheSpire2.Localization.Message GetBossLabel()
    {
        var runState = BossRunStateField?.GetValue(Control) as IRunState;
        if (runState == null) return SayTheSpire2.Localization.Message.Localized("ui", "LABELS.BOSS");

        var boss1 = runState.Act.BossEncounter;
        var boss2 = runState.Act.SecondBossEncounter;

        if (boss2 != null && !ShouldOnlyShowSecondBoss(runState))
            return SayTheSpire2.Localization.Message.Localized("ui", "TOPBAR.BOSS_DUAL", new { name1 = boss1.Title.GetFormattedText(), name2 = boss2.Title.GetFormattedText() });

        var activeBoss = (boss2 != null && ShouldOnlyShowSecondBoss(runState)) ? boss2 : boss1;
        return SayTheSpire2.Localization.Message.Localized("ui", "TOPBAR.BOSS", new { name = activeBoss.Title.GetFormattedText() });
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

    // The Dread run's Dominion for this act, when the boss has one. In
    // Dread the game shows it on the boss icon in place of the plain
    // "the boss of this act" tip, or after the two-boss tip in the last act
    // (Dread is ascension 10, so the last act always has two bosses).
    private DreadRunView? GetDominion()
    {
        var dread = DreadRunView.FromRunState(BossRunStateField?.GetValue(Control) as IRunState);
        return dread is { HasDominion: true } ? dread : null;
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

            // Dread shows no single-boss tip; the Dominion stands in for it
            // (added by the callers).
            if (GetDominion() != null)
                return (null, null);

            var activeBoss = (boss2 != null && ShouldOnlyShowSecondBoss(runState)) ? boss2 : boss1;
            var singleTitle = new LocString("static_hover_tips", "BOSS.title");
            singleTitle.Add("BossName", activeBoss.Title);
            var singleDesc = new LocString("static_hover_tips", "BOSS.description");
            singleDesc.Add("BossName", activeBoss.Title);
            return (singleTitle.GetFormattedText(), singleDesc.GetFormattedText());
        }
        catch (System.Exception e) { Log.Info($"[AccessibilityMod] Boss tooltip failed: {e.Message}"); return (null, null); }
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
        catch (System.Exception e) { Log.Info($"[AccessibilityMod] Room tip prefix failed: {e.Message}"); return null; }
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
        catch (System.Exception e) { Log.Info($"[AccessibilityMod] Boss tip prefix failed: {e.Message}"); return null; }
    }

    private static bool ShouldOnlyShowSecondBoss(IRunState runState)
    {
        return runState.Map.SecondBossMapPoint != null
            && runState.CurrentMapPoint == runState.Map.BossMapPoint;
    }
}
