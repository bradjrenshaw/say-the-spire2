namespace SayTheSpire2.Settings;

/// <summary>
/// Marker interface for the Nullable* setting family
/// (<see cref="NullableBoolSetting"/>, <see cref="NullableIntSetting"/>,
/// <see cref="NullableStringSetting"/>, <see cref="NullableChoiceSetting"/>).
/// Lets the override-resolution code in <see cref="UI.Announcements.AnnouncementContext"/>
/// check whether a per-element / per-buffer override is explicitly set
/// without knowing the concrete value type.
/// </summary>
public interface INullableSetting
{
    bool IsOverridden { get; }
}
