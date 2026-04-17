using System.Collections.Generic;
using System.Linq;

namespace SayTheSpire2.Settings;

public class CategorySetting : Setting
{
    private readonly List<Setting> _children = new();
    public override bool IncludeInPath { get; }

    public IReadOnlyList<Setting> Children => _children;

    /// <summary>
    /// When true, the settings screen prepends a "Reset to defaults" action
    /// that clears every descendant NullableBoolSetting back to null (inherit).
    /// Set by AnnouncementRegistry on per-element override categories.
    /// </summary>
    public bool HasResetAction { get; set; }

    public CategorySetting(string key, string label, bool includeInPath = true, string localizationKey = "")
        : base(key, label, localizationKey)
    {
        IncludeInPath = includeInPath;
    }

    public void Add(Setting child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public bool Remove(Setting child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }
        return false;
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
