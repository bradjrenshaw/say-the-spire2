using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SayTheSpire2.Input;

namespace SayTheSpire2.Settings;

public class BindingSetting : Setting
{
    private readonly InputAction _action;
    private bool _loading;

    public InputAction Action => _action;

    public BindingSetting(InputAction action)
        : base(action.Key, action.Label)
    {
        _action = action;
        _action.BindingsChanged += () =>
        {
            if (!_loading)
                ModSettings.MarkDirty();
        };
    }

    public override object? BoxedValue
    {
        get
        {
            return _action.Bindings.Select(b => new Dictionary<string, string>
            {
                { "type", b.Type },
                { "binding", b.Serialize() }
            }).ToList();
        }
    }

    public override void LoadValue(object? value)
    {
        if (value is not JsonElement element || element.ValueKind != JsonValueKind.Array)
            return;

        _loading = true;
        try
        {
            _action.ClearBindings();

            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (!item.TryGetProperty("type", out var typeProp) ||
                    !item.TryGetProperty("binding", out var bindingProp))
                    continue;

                var type = typeProp.GetString();
                var binding = bindingProp.GetString();
                if (type == null || binding == null)
                    continue;

                var parsed = InputBinding.Deserialize(type, binding);
                if (parsed != null)
                    _action.AddBinding(parsed);
            }

        }
        finally
        {
            _loading = false;
        }
    }
}
