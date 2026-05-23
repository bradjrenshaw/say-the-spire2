using System;

namespace SayTheSpire2.Settings;

/// <summary>
/// A string setting that can be "inherited" from a parent/global StringSetting.
/// Stores string? internally — null means inherit, any non-null string (including
/// empty) means explicit override. This distinction matters: a user who types
/// and clears the field has set an explicit empty string, and global changes
/// should no longer propagate. Only the category-level "Reset to defaults"
/// clears back to null.
/// </summary>
public class NullableStringSetting : Setting, INullableSetting
{
    public StringSetting Fallback { get; }
    public string? LocalValue { get; private set; }

    public event Action<string>? ResolvedChanged;

    public NullableStringSetting(string key, string label, StringSetting fallback, string localizationKey = "")
        : base(key, label, localizationKey)
    {
        Fallback = fallback;
        fallback.Changed += OnFallbackChanged;
    }

    public bool IsOverridden => LocalValue != null;
    public string Resolved => LocalValue ?? Fallback.Value;

    public void SetExplicit(string value)
    {
        var prev = Resolved;
        LocalValue = value;
        ModSettings.MarkDirty();
        if (prev != value)
            ResolvedChanged?.Invoke(value);
    }

    public void Reset()
    {
        if (LocalValue == null) return;
        var prev = LocalValue;
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
            string s => s,
            null => null,
            _ => null,
        };
    }

    private void OnFallbackChanged(string newValue)
    {
        if (LocalValue == null)
            ResolvedChanged?.Invoke(newValue);
    }
}
