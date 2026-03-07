using System;

namespace SayTheSpire2.Settings;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventSettingsAttribute : Attribute
{
    public string Key { get; }
    public string Label { get; }
    public bool DefaultAnnounce { get; }
    public bool DefaultBuffer { get; }

    public EventSettingsAttribute(string key, string label, bool defaultAnnounce = true, bool defaultBuffer = true)
    {
        Key = key;
        Label = label;
        DefaultAnnounce = defaultAnnounce;
        DefaultBuffer = defaultBuffer;
    }
}
