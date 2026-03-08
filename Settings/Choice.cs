namespace SayTheSpire2.Settings;

public class Choice
{
    public string Key { get; }
    public string Label { get; }
    public object? Metadata { get; }

    public Choice(string key, string label, object? metadata = null)
    {
        Key = key;
        Label = label;
        Metadata = metadata;
    }
}
