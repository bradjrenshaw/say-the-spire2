using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.Events;

public static class EventDispatcher
{
    public static bool VerboseLogging { get; set; } = true;

    private static readonly List<(GameEvent evt, ulong timestamp)> _pending = new();

    public static void Enqueue(GameEvent evt)
    {
        var message = evt.GetMessage();
        if (string.IsNullOrEmpty(message)) return;

        var timestamp = Time.GetTicksMsec();

        if (VerboseLogging)
        {
            var caller = new StackTrace(1, false);
            var callerFrame = caller.GetFrame(0);
            var callerMethod = callerFrame?.GetMethod();
            var callerInfo = callerMethod != null
                ? $"{callerMethod.DeclaringType?.Name}.{callerMethod.Name}"
                : "unknown";
            Log.Info($"[EventDebug] Enqueue: type={evt.GetType().Name} caller={callerInfo} msg=\"{message}\" t={timestamp}");
        }

        // Insertion sort by timestamp
        int i = _pending.Count;
        while (i > 0 && _pending[i - 1].timestamp > timestamp)
            i--;
        _pending.Insert(i, (evt, timestamp));
    }

    public static void Flush()
    {
        if (_pending.Count == 0) return;

        for (int i = 0; i < _pending.Count; i++)
        {
            var evt = _pending[i].evt;
            var message = evt.GetMessage();
            if (string.IsNullOrEmpty(message)) continue;

            var attr = (EventSettingsAttribute?)Attribute.GetCustomAttribute(
                evt.GetType(), typeof(EventSettingsAttribute));

            bool announce = attr != null ? EventRegistry.ShouldAnnounce(attr.Key) : evt.ShouldAnnounce();
            bool buffer = attr != null ? EventRegistry.ShouldBuffer(attr.Key) : evt.ShouldAddToBuffer();

            if (VerboseLogging)
            {
                Log.Info($"[EventDebug]   Flush: msg=\"{message}\" announce={announce} buffer={buffer} settingsKey={attr?.Key ?? "none"}");
            }

            if (announce)
            {
                SpeechManager.Output(Message.Raw(message), interrupt: false);
            }

            if (buffer)
            {
                var buf = BufferManager.Instance.GetBuffer("events");
                buf?.Add(message);
                BufferManager.Instance.EnableBuffer("events", true);
            }
        }

        _pending.Clear();
    }
}
