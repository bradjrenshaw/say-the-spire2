using System;
using SayTheSpire2.Buffers;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.Events;

public static class EventDispatcher
{
    public static void Enqueue(GameEvent evt)
    {
        var message = evt.GetMessage();
        if (string.IsNullOrEmpty(message)) return;

        var attr = (EventSettingsAttribute?)Attribute.GetCustomAttribute(
            evt.GetType(), typeof(EventSettingsAttribute));

        bool announce = attr != null ? EventRegistry.ShouldAnnounce(attr.Key) : evt.ShouldAnnounce();
        bool buffer = attr != null ? EventRegistry.ShouldBuffer(attr.Key) : evt.ShouldAddToBuffer();

        if (announce)
        {
            SpeechManager.Output(message, interrupt: false);
        }

        if (buffer)
        {
            var buf = BufferManager.Instance.GetBuffer("events");
            buf?.Add(message);
            BufferManager.Instance.EnableBuffer("events", true);
        }
    }
}
