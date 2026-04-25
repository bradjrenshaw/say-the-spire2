using System;

namespace SayTheSpire2.Settings;

public class IntSetting : Setting
{
    public int Default { get; }
    public int Value { get; private set; }
    public int Min { get; }
    public int Max { get; }
    public int Step { get; }

    public event Action<int>? Changed;

    public IntSetting(string key, string label, int defaultValue = 0, int min = 0, int max = 100, int step = 1, string localizationKey = "")
        : base(key, label, localizationKey)
    {
        Default = defaultValue;
        Value = defaultValue;
        Min = min;
        Max = max;
        Step = step;
    }

    public int Get() => Value;

    public void Set(int value)
    {
        var clamped = Math.Clamp(value, Min, Max);
        if (Value == clamped) return;
        Value = clamped;
        ModSettings.MarkDirty();
        Changed?.Invoke(clamped);
    }

    public override object? BoxedValue => Value;

    public override void LoadValue(object? value)
    {
        int newValue;
        if (value is int i)
            newValue = Math.Clamp(i, Min, Max);
        else if (value is long l)
            newValue = Math.Clamp((int)l, Min, Max);
        else
            return;

        if (newValue != Value)
        {
            Value = newValue;
            Changed?.Invoke(newValue);
        }
    }
}
