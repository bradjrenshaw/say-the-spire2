using MegaCrit.Sts2.Core.Rooms;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("room_entered", "Room Entered", category: "Other")]
public class RoomEnteredEvent : GameEvent
{
    private readonly RoomType _roomType;

    public RoomEnteredEvent(RoomType roomType)
    {
        _roomType = roomType;
    }

    public static void RegisterSettings(CategorySetting category)
    {
        category.Add(new BoolSetting("announce_combat", "Announce Combat", true));
        category.Add(new BoolSetting("announce_elite", "Announce Elite Combat", true));
        category.Add(new BoolSetting("announce_boss", "Announce Boss Combat", true));
        category.Add(new BoolSetting("announce_treasure", "Announce Treasure Chest", true));
        category.Add(new BoolSetting("announce_shop", "Announce Shop", true));
        category.Add(new BoolSetting("announce_event", "Announce Event", true));
        category.Add(new BoolSetting("announce_rest_site", "Announce Rest Site", true));
    }

    public override Message? GetMessage()
    {
        return _roomType switch
        {
            RoomType.Monster => Message.Localized("ui", "EVENT.ROOM_MONSTER"),
            RoomType.Elite => Message.Localized("ui", "EVENT.ROOM_ELITE"),
            RoomType.Boss => Message.Localized("ui", "EVENT.ROOM_BOSS"),
            RoomType.Treasure => Message.Localized("ui", "EVENT.ROOM_TREASURE"),
            RoomType.Shop => Message.Localized("ui", "EVENT.ROOM_SHOP"),
            RoomType.Event => Message.Localized("ui", "EVENT.ROOM_EVENT"),
            RoomType.RestSite => Message.Localized("ui", "EVENT.ROOM_REST"),
            _ => null,
        };
    }

    public override bool ShouldAnnounce()
    {
        return _roomType switch
        {
            RoomType.Monster => ModSettings.GetValue<bool>("events.room_entered.announce_combat"),
            RoomType.Elite => ModSettings.GetValue<bool>("events.room_entered.announce_elite"),
            RoomType.Boss => ModSettings.GetValue<bool>("events.room_entered.announce_boss"),
            RoomType.Treasure => ModSettings.GetValue<bool>("events.room_entered.announce_treasure"),
            RoomType.Shop => ModSettings.GetValue<bool>("events.room_entered.announce_shop"),
            RoomType.Event => ModSettings.GetValue<bool>("events.room_entered.announce_event"),
            RoomType.RestSite => ModSettings.GetValue<bool>("events.room_entered.announce_rest_site"),
            _ => true,
        };
    }
}
