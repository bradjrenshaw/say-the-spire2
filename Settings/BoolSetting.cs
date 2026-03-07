namespace SayTheSpire2.Settings;

public class BoolSetting : Setting
{
    public bool Default { get; }
    public bool Value { get; private set; }

    public BoolSetting(string key, string label, bool defaultValue = true)
        : base(key, label)
    {
        Default = defaultValue;
        Value = defaultValue;
    }

    public bool Get() => Value;

    public void Set(bool value)
    {
        Value = value;
        ModSettings.MarkDirty();
    }

    public override object? BoxedValue => Value;

    public override void LoadValue(object? value)
    {
        if (value is bool b)
            Value = b;
    }
}
