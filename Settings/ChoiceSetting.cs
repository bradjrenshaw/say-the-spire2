using System;
using System.Collections.Generic;
using System.Linq;

namespace SayTheSpire2.Settings;

public class ChoiceSetting : Setting
{
    private readonly List<Choice> _options;

    public string DefaultKey { get; }
    public string Value { get; private set; }
    public IReadOnlyList<Choice> Options => _options;

    public event Action<string>? Changed;

    public ChoiceSetting(string key, string label, string defaultKey, List<Choice> options)
        : base(key, label)
    {
        DefaultKey = defaultKey;
        Value = defaultKey;
        _options = options;
    }

    public string Get() => Value;

    public Choice? GetSelected() => _options.FirstOrDefault(o => o.Key == Value);

    public void Set(string key)
    {
        if (Value == key) return;
        if (_options.All(o => o.Key != key)) return;
        Value = key;
        ModSettings.MarkDirty();
        Changed?.Invoke(key);
    }

    public void SetOptions(List<Choice> options)
    {
        _options.Clear();
        _options.AddRange(options);

        // If current value is no longer valid, reset to default or first option
        if (_options.All(o => o.Key != Value))
        {
            var fallback = _options.FirstOrDefault(o => o.Key == DefaultKey)
                ?? _options.FirstOrDefault();
            var newValue = fallback?.Key ?? DefaultKey;
            if (newValue != Value)
            {
                Value = newValue;
                Changed?.Invoke(Value);
            }
        }
    }

    public override object? BoxedValue => Value;

    public override void LoadValue(object? value)
    {
        if (value is string s && s != Value)
        {
            // Accept the value even if not in current options —
            // options may be populated later (e.g., runtime voice list)
            Value = s;
            Changed?.Invoke(s);
        }
    }
}
