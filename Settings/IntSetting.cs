namespace SayTheSpire2.Settings;

public class IntSetting : Setting
{
    public int Default { get; }
    public int Value { get; private set; }
    public int Min { get; }
    public int Max { get; }

    public IntSetting(string key, string label, int defaultValue = 0, int min = 0, int max = 100)
        : base(key, label)
    {
        Default = defaultValue;
        Value = defaultValue;
        Min = min;
        Max = max;
    }

    public int Get() => Value;

    public void Set(int value)
    {
        Value = System.Math.Clamp(value, Min, Max);
        ModSettings.MarkDirty();
    }

    public override object? BoxedValue => Value;

    public override void LoadValue(object? value)
    {
        if (value is int i)
            Value = System.Math.Clamp(i, Min, Max);
        else if (value is long l)
            Value = System.Math.Clamp((int)l, Min, Max);
    }
}
