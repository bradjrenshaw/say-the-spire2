using System;
using System.Collections.Generic;
using System.Linq;

namespace SayTheSpire2.Settings;

/// <summary>
/// A choice setting that can be "inherited" from a parent/global ChoiceSetting.
/// Stores the selected choice key as string? — null = inherit, any non-null
/// string = explicit override. Options mirror the fallback's options list
/// (stays in sync when the fallback's options change at runtime).
/// </summary>
public class NullableChoiceSetting : Setting, INullableSetting
{
    public ChoiceSetting Fallback { get; }
    public string? LocalValue { get; private set; }

    public event Action<string>? ResolvedChanged;

    public IReadOnlyList<Choice> Options => Fallback.Options;

    public NullableChoiceSetting(string key, string label, ChoiceSetting fallback, string localizationKey = "")
        : base(key, label, localizationKey)
    {
        Fallback = fallback;
        fallback.Changed += OnFallbackChanged;
    }

    public bool IsOverridden => LocalValue != null;
    public string Resolved => LocalValue ?? Fallback.Value;

    public Choice? GetSelectedChoice() => Options.FirstOrDefault(o => o.Key == Resolved);

    public void SetExplicit(string key)
    {
        if (Options.All(o => o.Key != key)) return;
        var prev = Resolved;
        LocalValue = key;
        ModSettings.MarkDirty();
        if (prev != key)
            ResolvedChanged?.Invoke(key);
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
