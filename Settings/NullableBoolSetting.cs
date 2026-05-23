using System;

namespace SayTheSpire2.Settings;

/// <summary>
/// A bool setting that can be "inherited" from a parent/global setting. Stores
/// bool? internally: null = inherit, true/false = explicit override. The UI
/// renders it as a regular checkbox showing the resolved value (user never
/// sees "inherit"); toggling always writes an explicit value. The "Reset"
/// action clears it back to null (inherit).
///
/// Used for per-element announcement overrides where each proxy type
/// can customize its announcements independently while following global
/// defaults for settings the user hasn't touched.
/// </summary>
public class NullableBoolSetting : Setting, INullableSetting
{
    public BoolSetting Fallback { get; }
    public bool? LocalValue { get; private set; }

    public event Action<bool>? ResolvedChanged;

    public NullableBoolSetting(string key, string label, BoolSetting fallback, string localizationKey = "")
        : base(key, label, localizationKey)
    {
        Fallback = fallback;
        fallback.Changed += OnFallbackChanged;
    }

    /// <summary>True if the user has explicitly overridden this setting.</summary>
    public bool IsOverridden => LocalValue.HasValue;

    /// <summary>The value that takes effect: explicit override if set, otherwise the global fallback.</summary>
    public bool Resolved => LocalValue ?? Fallback.Value;

    /// <summary>Writes an explicit value. Subsequent global changes no longer propagate.</summary>
    public void SetExplicit(bool value)
    {
        var prev = Resolved;
        LocalValue = value;
        ModSettings.MarkDirty();
        if (prev != value)
            ResolvedChanged?.Invoke(value);
    }

    /// <summary>Clears the override so the setting follows the global fallback again.</summary>
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
            bool b => b,
            null => null,
            _ => null,
        };
    }

    private void OnFallbackChanged(bool newValue)
    {
        if (!LocalValue.HasValue)
            ResolvedChanged?.Invoke(newValue);
    }
}
