using System;
using System.Collections.Generic;
using System.Diagnostics;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Events;

namespace SayTheSpire2.Diagnostics;

/// <summary>
/// Frame profiler aimed at the failure mode plain per-section timing misses:
/// intermittent stutter. Per-call averages stay flat even when the game
/// hitches, because a single 30 ms GC pause is invisible once divided across
/// hundreds of fast frames.
///
/// It measures three things across the <i>full</i> frame cycle (begin-to-begin,
/// so it captures the game's work and GC, not just ours): the inter-frame
/// period (dropped frames surface as spikes), process-wide bytes allocated, and
/// GC collection counts. Each spike is tagged with the collections that fired
/// during it, and the window summary reports how many dropped frames coincided
/// with a GC. That discriminates the cause directly: if spikes carry gen0/1/2
/// ticks, it's GC pauses; if they don't, it's game-side per-frame work. Our own
/// per-section time and allocation (always tiny) are tracked separately so we
/// can confirm the mod isn't the source.
///
/// Gated on the Advanced / Performance Profiling toggle
/// (<see cref="EventDispatcher.Profiling"/>); allocation-free on the hot path.
/// </summary>
public static class Profiler
{
    /// <summary>A frame period above this (ms) is treated as a stutter.</summary>
    private const double SpikeMs = 25.0;

    /// <summary>Frames per rolling summary (~10s at 60 fps).</summary>
    private const int WindowFrames = 600;

    public static bool Enabled => EventDispatcher.Profiling;

    private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

    // Cycle anchors (sampled once per BeginFrame, describe the cycle that just
    // completed so period / alloc / GC all line up).
    private static bool _wasEnabled;
    private static long _lastBeginTs;
    private static long _lastTotalAlloc;
    private static readonly int[] _lastGc = new int[3];

    // Our-ProcessPostfix-only section scratch (mod cost, not game cost).
    private static long _sectionStartAlloc;
    private static readonly Dictionary<string, long> _frameSectionTicks = new();
    private static readonly Dictionary<string, long> _frameSectionAlloc = new();

    // Window aggregates.
    private static int _windowFrames;
    private static double _windowPeriodSumMs;
    private static double _windowPeriodMaxMs;
    private static int _windowDropped;
    private static int _windowDroppedWithGc;
    private static long _windowAllocTotal;
    private static long _windowAllocMaxCycle;
    private static readonly int[] _gcWindowStart = new int[3];
    private static readonly List<double> _windowPeriods = new();
    private static readonly Dictionary<string, SectionAgg> _windowSections = new();

    private struct SectionAgg
    {
        public double TotalMs;
        public double MaxMs;
        public long TotalAlloc;
        public int Count;
    }

    /// <summary>
    /// Times and measures the allocations of the wrapped block, accumulating
    /// into the current frame. Use with <c>using</c>. Inert when profiling is
    /// off (no timestamp / counter reads).
    /// </summary>
    public readonly struct Scope : IDisposable
    {
        private readonly string? _name;
        private readonly long _startTs;
        private readonly long _startAlloc;

        internal Scope(string? name)
        {
            _name = name;
            if (name != null)
            {
                _startTs = Stopwatch.GetTimestamp();
                _startAlloc = GC.GetTotalAllocatedBytes(false);
            }
            else
            {
                _startTs = 0;
                _startAlloc = 0;
            }
        }

        public void Dispose()
        {
            if (_name == null) return;
            long ticks = Stopwatch.GetTimestamp() - _startTs;
            long bytes = GC.GetTotalAllocatedBytes(false) - _startAlloc;

            _frameSectionTicks.TryGetValue(_name, out var t);
            _frameSectionTicks[_name] = t + ticks;
            _frameSectionAlloc.TryGetValue(_name, out var b);
            _frameSectionAlloc[_name] = b + bytes;
        }
    }

    public static Scope Section(string name) => new(Enabled ? name : null);

    public static void BeginFrame()
    {
        if (!Enabled)
        {
            _wasEnabled = false;
            return;
        }

        long now = Stopwatch.GetTimestamp();
        long allocNow = GC.GetTotalAllocatedBytes(false);
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);

        if (!_wasEnabled)
        {
            // Just turned on (or first frame) — seed anchors so a stale sample
            // doesn't manufacture a bogus opening spike.
            _wasEnabled = true;
            ResetWindow();
            _lastBeginTs = now;
            _lastTotalAlloc = allocNow;
            _lastGc[0] = gc0; _lastGc[1] = gc1; _lastGc[2] = gc2;
            Log.Info($"[Profile] Frame profiler started (spike>{SpikeMs:F0}ms, window={WindowFrames} frames).");
        }
        else
        {
            // Everything below describes the cycle that just completed.
            double periodMs = (now - _lastBeginTs) * TicksToMs;
            long allocCycle = allocNow - _lastTotalAlloc;
            int d0 = gc0 - _lastGc[0], d1 = gc1 - _lastGc[1], d2 = gc2 - _lastGc[2];
            bool gcHit = (d0 | d1 | d2) != 0;

            _windowFrames++;
            _windowPeriodSumMs += periodMs;
            if (periodMs > _windowPeriodMaxMs) _windowPeriodMaxMs = periodMs;
            _windowPeriods.Add(periodMs);
            _windowAllocTotal += allocCycle;
            if (allocCycle > _windowAllocMaxCycle) _windowAllocMaxCycle = allocCycle;

            if (periodMs > SpikeMs)
            {
                _windowDropped++;
                if (gcHit) _windowDroppedWithGc++;
                Log.Info($"[Profile] SPIKE {periodMs:F1}ms (alloc {Mb(allocCycle)}, GC gen0+{d0}/gen1+{d1}/gen2+{d2})");
            }

            if (_windowFrames >= WindowFrames)
                FlushWindow();
        }

        _lastBeginTs = now;
        _lastTotalAlloc = allocNow;
        _lastGc[0] = gc0; _lastGc[1] = gc1; _lastGc[2] = gc2;

        // Reset our section scratch for the upcoming ProcessPostfix.
        _sectionStartAlloc = allocNow;
        _frameSectionTicks.Clear();
        _frameSectionAlloc.Clear();
    }

    public static void EndFrame()
    {
        if (!Enabled) return;

        // Roll our (mod-only) section costs into the window. Period / alloc /
        // GC for the cycle are handled in BeginFrame.
        foreach (var kv in _frameSectionTicks)
        {
            double ms = kv.Value * TicksToMs;
            _frameSectionAlloc.TryGetValue(kv.Key, out var bytes);
            _windowSections.TryGetValue(kv.Key, out var agg);
            agg.TotalMs += ms;
            if (ms > agg.MaxMs) agg.MaxMs = ms;
            agg.TotalAlloc += bytes;
            agg.Count++;
            _windowSections[kv.Key] = agg;
        }
    }

    private static void FlushWindow()
    {
        double wallMs = _windowPeriodSumMs;
        double avgMs = _windowFrames > 0 ? _windowPeriodSumMs / _windowFrames : 0;
        double p99 = Percentile(_windowPeriods, 0.99);
        double rateMbs = wallMs > 0 ? _windowAllocTotal / 1048576.0 / (wallMs / 1000.0) : 0;

        Log.Info($"[Profile] === window: {_windowFrames} frames over {wallMs / 1000.0:F1}s ===");
        Log.Info($"[Profile] frame ms: avg={avgMs:F1} max={_windowPeriodMaxMs:F1} p99={p99:F1} dropped(>{SpikeMs:F0}ms)={_windowDropped} (GC-coincident={_windowDroppedWithGc})");
        Log.Info($"[Profile] alloc (process): total={Mb(_windowAllocTotal)} avg={Kb(_windowAllocTotal / Math.Max(1, _windowFrames))}/frame max={Mb(_windowAllocMaxCycle)}/frame rate={rateMbs:F1}MB/s");
        Log.Info($"[Profile] GC collections: gen0={GC.CollectionCount(0) - _gcWindowStart[0]} gen1={GC.CollectionCount(1) - _gcWindowStart[1]} gen2={GC.CollectionCount(2) - _gcWindowStart[2]}");

        // Our sections, ordered by total time descending (mod cost only).
        var ordered = new List<KeyValuePair<string, SectionAgg>>(_windowSections);
        ordered.Sort((a, b) => b.Value.TotalMs.CompareTo(a.Value.TotalMs));
        Log.Info("[Profile] mod sections (avg ms / max ms / avg alloc):");
        foreach (var kv in ordered)
        {
            var s = kv.Value;
            int n = Math.Max(1, s.Count);
            Log.Info($"[Profile]   {kv.Key}: {s.TotalMs / n:F3} / {s.MaxMs:F3} / {Kb(s.TotalAlloc / n)}");
        }

        ResetWindow();
    }

    private static void ResetWindow()
    {
        _windowFrames = 0;
        _windowPeriodSumMs = 0;
        _windowPeriodMaxMs = 0;
        _windowDropped = 0;
        _windowDroppedWithGc = 0;
        _windowAllocTotal = 0;
        _windowAllocMaxCycle = 0;
        _windowPeriods.Clear();
        _windowSections.Clear();
        for (int g = 0; g < 3; g++) _gcWindowStart[g] = GC.CollectionCount(g);
    }

    private static double Percentile(List<double> values, double p)
    {
        if (values.Count == 0) return 0;
        var copy = new List<double>(values);
        copy.Sort();
        int idx = (int)Math.Ceiling(p * copy.Count) - 1;
        idx = Math.Clamp(idx, 0, copy.Count - 1);
        return copy[idx];
    }

    private static string Mb(long bytes) => $"{bytes / 1048576.0:F2}MB";
    private static string Kb(long bytes) => $"{bytes / 1024.0:F0}KB";
}
