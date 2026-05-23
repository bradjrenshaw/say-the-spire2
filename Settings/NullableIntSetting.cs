using System;

namespace SayTheSpire2.Settings;

/// <summary>
/// An int setting that can be "inherited" from a parent/global IntSetting.
/// Stores int? internally: null = inherit, any int = explicit override.
/// UI shows the resolved value as a regular slider; toggling always writes
/// explicit; the category "Reset to defaults" action clears it back to null.
/// Same cascade semantics as NullableBoolSetting.
/// </summary>
public class NullableIntSetting : Setting, INullableSetting
{
    public IntSetting Fallback { get; }
    public int? LocalValue { get; private set; }

    public event Action<int>? ResolvedChanged;

    public NullableIntSetting(string key, string label, IntSetting fallback, string localizationKey = "")
        : base(key, label, localizationKey)
    {
        Fallback = fallback;
        fallback.Changed += OnFallbackChanged;
    }

    public bool IsOverridden => LocalValue.HasValue;
    public int Resolved => LocalValue ?? Fallback.Value;

    public void SetExplicit(int value)
    {
        var clamped = Math.Clamp(value, Fallback.Min, Fallback.Max);
        var prev = Resolved;
        LocalValue = clamped;
        ModSettings.MarkDirty();
        if (prev != clamped)
            ResolvedChanged?.Invoke(clamped);
    }

    public void Reset()
    {
        if (!LocalValue.HasValue) return;
        var prev = LocalValue.Value;
        LocalValue = null;
        ModSettings.MarkDirty();
        if (prev != Fallback.Value)
            ResolvedChanged?.Invoke(Fallback.Value);
    }

    public override object? BoxedValue => LocalValue;

    public override void LoadValue(object? value)
    {
        LocalValue = value switch
        {
            int i => Math.Clamp(i, Fallback.Min, Fallback.Max),
            long l => Math.Clamp((int)l, Fallback.Min, Fallback.Max),
            null => null,
            _ => null,
        };
    }

    private void OnFallbackChanged(int newValue)
    {
        if (!LocalValue.HasValue)
            ResolvedChanged?.Invoke(newValue);
    }
}
