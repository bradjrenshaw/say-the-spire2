using System;
using System.Collections.Generic;

namespace SayTheSpire2.Settings;

public class StringSetting : Setting
{
    public string Default { get; }
    public string Value { get; private set; }
    public IReadOnlyList<string>? Options { get; set; }

    public event Action<string>? Changed;

    public StringSetting(string key, string label, string defaultValue = "")
        : base(key, label)
    {
        Default = defaultValue;
        Value = defaultValue;
    }

    public string Get() => Value;

    public void Set(string value)
    {
        if (Value == value) return;
        Value = value;
        ModSettings.MarkDirty();
        Changed?.Invoke(value);
    }

    public override object? BoxedValue => Value;

    public override void LoadValue(object? value)
    {
        if (value is string s && s != Value)
        {
            Value = s;
            Changed?.Invoke(s);
        }
    }
}
