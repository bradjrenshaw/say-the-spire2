using System;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Opts an <see cref="Announcement"/> subclass into the top-level
/// <c>Announcements/</c> settings tree. Without this attribute the global
/// category is still created (per-element overrides fall back to its
/// defaults) but marked <see cref="Settings.Setting.Hidden"/> so it won't
/// appear in the UI — users interact with those announcements only through
/// per-element override screens and the reorder rows.
///
/// Apply to announcements whose behavior is uniform enough across contexts
/// that a single global toggle reads naturally (e.g. label, position,
/// tooltip, type). Skip announcements tied to a specific mechanic or
/// context (e.g. HP, energy cost, price, monster intents) where a global
/// knob would confuse more than help.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ShowInGlobalSettingsAttribute : Attribute
{
}
