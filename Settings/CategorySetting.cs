using System.Collections.Generic;
using System.Linq;

namespace SayTheSpire2.Settings;

public class CategorySetting : Setting
{
    private readonly List<Setting> _children = new();

    public IReadOnlyList<Setting> Children => _children;

    public CategorySetting(string key, string label) : base(key, label)
    {
    }

    public void Add(Setting child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public T? Get<T>(string key) where T : Setting
    {
        return _children.OfType<T>().FirstOrDefault(c => c.Key == key);
    }

    public Setting? GetByKey(string key)
    {
        return _children.FirstOrDefault(c => c.Key == key);
    }

    public override object? BoxedValue => null;
    public override void LoadValue(object? value) { }
}
