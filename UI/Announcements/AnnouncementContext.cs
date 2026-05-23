using SayTheSpire2.Settings;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Supplied to <see cref="Announcement.Render"/> so announcements can resolve
/// per-announcement settings with a two-level override cascade:
/// inner element → outer proxy → global default (most specific wins).
///
/// <para>The inner key is derived from <see cref="UIElement.AnnouncementOrderType"/>
/// which also governs ordering — for a merchant slot wrapping a potion, the
/// inner key is <c>potion</c>. The outer key is the focused element's actual
/// type (e.g. <c>ProxyMerchantSlot</c> → <c>merchant_slot</c>). When the two
/// match — a non-composite proxy, or a composite whose inner is null — only
/// one lookup happens.</para>
///
/// <para>Inner takes priority because it's the more specific context: if the
/// user explicitly disables price on cards, a merchant_slot "enable prices"
/// override shouldn't silently re-enable it. The outer still acts as the
/// natural "disable price on all shop items" knob as long as inner stays
/// on inherit.</para>
/// </summary>
public sealed class AnnouncementContext
{
    /// <summary>
    /// The element currently being composed. Set only in focus context;
    /// null in buffer context (buffers operate on a model, not a focused
    /// control).
    /// </summary>
    public UIElement? Element { get; }

    /// <summary>
    /// Element key derived from <see cref="UIElement.AnnouncementOrderType"/>.
    /// Drives ordering and the second-priority override lookup. Null in
    /// buffer context.
    /// </summary>
    public string? ElementKey { get; }

    /// <summary>
    /// Element key derived from the focused element's actual type. Null when
    /// it matches <see cref="ElementKey"/> (non-composite proxies). Second
    /// priority in the override cascade — inner wins if explicitly set.
    /// </summary>
    public string? OuterKey { get; }

    /// <summary>
    /// Buffer identity in buffer context (e.g. "card"). Null in focus
    /// context. When set, override lookups go through
    /// <c>buffers.{BufferKey}.announcements.{ann}.{setting}/</c> instead of
    /// the per-element <c>ui.*</c> cascade.
    /// </summary>
    public string? BufferKey { get; }

    /// <summary>Focus context — composes an element's spoken focus message.</summary>
    public AnnouncementContext(UIElement element)
    {
        Element = element;
        var innerType = element.AnnouncementOrderType;
        ElementKey = AnnouncementRegistry.DeriveElementKey(innerType);

        var outerType = element.GetType();
        if (outerType != innerType)
        {
            var outerKey = AnnouncementRegistry.DeriveElementKey(outerType);
            if (outerKey != ElementKey)
                OuterKey = outerKey;
        }
    }

    /// <summary>Buffer context — composes the contents of a buffer with the given key.</summary>
    public static AnnouncementContext ForBuffer(string bufferKey) =>
        new AnnouncementContext(bufferKey: bufferKey);

    private AnnouncementContext(string bufferKey)
    {
        BufferKey = bufferKey;
    }

    /// <summary>
    /// Hotkey identity (the action key, e.g. "announce_hp"). When set,
    /// override lookups go through <c>hotkeys.{HotkeyKey}.{setting}/</c> —
    /// flatter than the ui/buffer cascades because a hotkey maps to exactly
    /// one announcement, so there's no per-announcement nesting.
    /// </summary>
    public string? HotkeyKey { get; }

    /// <summary>
    /// Standalone context for invoking an announcement outside of focus or
    /// buffer composition. Setting lookups skip per-element and per-buffer
    /// cascades and resolve straight from the global Announcements category.
    /// </summary>
    public static AnnouncementContext Global() => new AnnouncementContext();

    /// <summary>
    /// Hotkey context (Ctrl+Y / Ctrl+H / etc). Per-hotkey option overrides
    /// resolve from <c>hotkeys.{hotkeyKey}.{setting}</c>, falling back to the
    /// global announcement setting — so a hotkey can be, say, compact even
    /// when the focus/buffer readout of the same announcement is verbose.
    /// </summary>
    public static AnnouncementContext ForHotkey(string hotkeyKey) =>
        new AnnouncementContext(hotkeyKey, isHotkey: true);

    private AnnouncementContext()
    {
        // All keys left null → ResolveBool/Int/String/Choice fall straight
        // through to the global Announcements setting.
    }

    private AnnouncementContext(string hotkeyKey, bool isHotkey)
    {
        HotkeyKey = hotkeyKey;
    }

    public bool ResolveBool(string announcementKey, string settingKey, bool defaultValue)
    {
        if (TryResolveOverride<NullableBoolSetting>(announcementKey, settingKey, out var ov))
            return ov!.LocalValue!.Value;

        var global = ModSettings.GetSetting<BoolSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public int ResolveInt(string announcementKey, string settingKey, int defaultValue)
    {
        if (TryResolveOverride<NullableIntSetting>(announcementKey, settingKey, out var ov))
            return ov!.LocalValue!.Value;

        var global = ModSettings.GetSetting<IntSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public string ResolveString(string announcementKey, string settingKey, string defaultValue)
    {
        if (TryResolveOverride<NullableStringSetting>(announcementKey, settingKey, out var ov))
            return ov!.LocalValue!;

        var global = ModSettings.GetSetting<StringSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    public string ResolveChoice(string announcementKey, string settingKey, string defaultValue)
    {
        if (TryResolveOverride<NullableChoiceSetting>(announcementKey, settingKey, out var ov))
            return ov!.LocalValue!;

        var global = ModSettings.GetSetting<ChoiceSetting>(
            $"announcements.{announcementKey}.{settingKey}");
        return global?.Value ?? defaultValue;
    }

    /// <summary>
    /// Walks the active override-scope chain (per-hotkey in hotkey context,
    /// per-buffer in buffer context, or inner-element then outer-element in
    /// focus context) looking for the first explicitly-overridden Nullable*
    /// setting. Returns true with the setting bound to
    /// <paramref name="overrideSetting"/> on match.
    /// </summary>
    private bool TryResolveOverride<TSetting>(string announcementKey, string settingKey, out TSetting? overrideSetting)
        where TSetting : Setting, INullableSetting
    {
        if (HotkeyKey != null)
        {
            // Hotkey scope is flat: hotkeys.{key}.{setting} (one announcement
            // per hotkey, so no per-announcement segment).
            var hotkeyOv = ModSettings.GetSetting<TSetting>(
                $"hotkeys.{HotkeyKey}.{settingKey}");
            if (hotkeyOv?.IsOverridden == true) { overrideSetting = hotkeyOv; return true; }
        }
        else if (BufferKey != null)
        {
            var bufferOv = ModSettings.GetSetting<TSetting>(
                $"buffers.{BufferKey}.announcements.{announcementKey}.{settingKey}");
            if (bufferOv?.IsOverridden == true) { overrideSetting = bufferOv; return true; }
        }
        else if (ElementKey != null)
        {
            var inner = ModSettings.GetSetting<TSetting>(
                $"ui.{ElementKey}.announcements.{announcementKey}.{settingKey}");
            if (inner?.IsOverridden == true) { overrideSetting = inner; return true; }

            if (OuterKey != null)
            {
                var outer = ModSettings.GetSetting<TSetting>(
                    $"ui.{OuterKey}.announcements.{announcementKey}.{settingKey}");
                if (outer?.IsOverridden == true) { overrideSetting = outer; return true; }
            }
        }
        // else: global context (no keys set) — fall through to the global setting.

        overrideSetting = null;
        return false;
    }
}
