namespace SayTheSpire2.Settings;

public abstract class Setting
{
    public string Key { get; }
    public string Label { get; }
    public CategorySetting? Parent { get; internal set; }

    /// <summary>
    /// Lower values sort first. Settings are sorted by priority, then alphabetically within each level.
    /// Default is 0.
    /// </summary>
    public int SortPriority { get; set; }

    protected Setting(string key, string label)
    {
        Key = key;
        Label = label;
    }

    public string FullPath
    {
        get
        {
            if (Parent == null || Parent.IsRoot)
                return Key;
            return $"{Parent.FullPath}.{Key}";
        }
    }

    /// <summary>
    /// Whether this is a root category (no parent, no key in path).
    /// </summary>
    public virtual bool IsRoot => false;

    /// <summary>
    /// Get the current value as object for serialization.
    /// Returns null for categories.
    /// </summary>
    public abstract object? BoxedValue { get; }

    /// <summary>
    /// Load a value from deserialized JSON.
    /// </summary>
    public abstract void LoadValue(object? value);
}
