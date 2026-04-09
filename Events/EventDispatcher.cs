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
    public static bool VerboseLogging { get; set; } = false;
    public static bool Profiling { get; set; } = false;

    private static readonly List<(GameEvent evt, ulong timestamp)> _pending = new();

    public static void Enqueue(GameEvent evt)
    {
        var message = evt.GetMessage();
        if (message == null || message.IsEmpty) return;

        var timestamp = Time.GetTicksMsec();

        if (VerboseLogging)
        {
            var caller = new StackTrace(1, false);
            var callerFrame = caller.GetFrame(0);
            var callerMethod = callerFrame?.GetMethod();
            var callerInfo = callerMethod != null
                ? $"{callerMethod.DeclaringType?.Name}.{callerMethod.Name}"
                : "unknown";
            Log.Info($"[EventDebug] Enqueue: type={evt.GetType().Name} caller={callerInfo} msg=\"{message.Resolve()}\" t={timestamp}");
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
            if (message == null || message.IsEmpty) continue;

            var attr = (EventSettingsAttribute?)Attribute.GetCustomAttribute(
                evt.GetType(), typeof(EventSettingsAttribute));

            // Check source filter first — if the creature source is filtered out, skip entirely
            if (attr != null && !EventRegistry.PassesSourceFilter(attr.Key, evt.Source))
                continue;

            bool announce = attr != null ? EventRegistry.ShouldAnnounce(attr.Key) && evt.ShouldAnnounce() : evt.ShouldAnnounce();
            bool buffer = attr != null ? EventRegistry.ShouldBuffer(attr.Key) && evt.ShouldAddToBuffer() : evt.ShouldAddToBuffer();

            if (VerboseLogging)
            {
                var resolved = message.Resolve();
                Log.Info($"[EventDebug]   Flush: msg=\"{resolved}\" announce={announce} buffer={buffer} settingsKey={attr?.Key ?? "none"}");
            }

            if (announce)
            {
                SpeechManager.Output(message, interrupt: false);
            }

            if (buffer)
            {
                var buf = BufferManager.Instance.GetBuffer("events");
                buf?.Add(message.Resolve());
                BufferManager.Instance.EnableBuffer("events", true);
            }
        }

        _pending.Clear();
    }
}
