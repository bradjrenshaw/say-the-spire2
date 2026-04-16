using SayTheSpire2.Localization;
using SayTheSpire2.Settings;

namespace SayTheSpire2.Events;

[EventSettings("hand_discarded", "Hand Discarded")]
public class HandDiscardedEvent : GameEvent
{
    public override Message? GetMessage() => Message.Localized("ui", "EVENT.HAND_DISCARDED");
}
