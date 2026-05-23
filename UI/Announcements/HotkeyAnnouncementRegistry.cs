using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Announcements;

/// <summary>
/// Ties each "announce_*" hotkey to an Announcement type so the spoken
/// readout flows through the same announcement system as focus and buffers.
/// Builds a "Hotkey Announcements" settings category exposing each hotkey's
/// announcement options (verbose, etc.) as per-hotkey overrides that cascade
/// from the global announcement defaults. Hotkeys whose announcement has no
/// options don't get a settings entry (nothing to configure), but still
/// route through the system via <see cref="Announce"/>.
/// </summary>
public static class HotkeyAnnouncementRegistry
{
    public readonly record struct Entry(
        string HotkeyKey,
        Type AnnouncementType,
        string LabelLocKey,
        string? EmptyLocKey = null,
        string Separator = ", ");

    // Setting keys on the global announcement category that aren't relevant
    // as per-hotkey overrides (a hotkey is one announcement, joined into one
    // utterance — there's no "don't announce" or inter-announcement suffix).
    private static readonly HashSet<string> NonOptionKeys = new() { "enabled", "include_suffix" };

    private static readonly Entry[] Entries =
    {
        // Labels reuse the existing keybinding loc keys (INPUT.ANNOUNCE_*),
        // which are already translated in every locale.
        new("announce_hp", typeof(HpAnnouncement), "INPUT.ANNOUNCE_HP"),
        new("announce_energy", typeof(ResourcesAnnouncement), "INPUT.ANNOUNCE_ENERGY"),
        new("announce_gold", typeof(GoldAnnouncement), "INPUT.ANNOUNCE_GOLD"),
        new("announce_block", typeof(BlockAnnouncement), "INPUT.ANNOUNCE_BLOCK"),
        new("announce_powers", typeof(PowersAnnouncement), "INPUT.ANNOUNCE_POWERS", "SPEECH.NO_POWERS"),
        new("announce_intents", typeof(AllIntentsAnnouncement), "INPUT.ANNOUNCE_INTENTS", "SPEECH.NO_ENEMIES", ". "),
        new("announce_summarized_intents", typeof(IncomingDamageAnnouncement), "INPUT.ANNOUNCE_SUMMARIZED_INTENTS"),
        new("announce_boss", typeof(BossAnnouncement), "INPUT.ANNOUNCE_BOSS"),
        new("announce_relic_counters", typeof(RelicCountersAnnouncement), "INPUT.ANNOUNCE_RELIC_COUNTERS", "SPEECH.NO_RELIC_COUNTERS", ". "),
    };

    private static readonly Dictionary<string, Entry> ByKey =
        Entries.ToDictionary(e => e.HotkeyKey);

    private const string RootLocKey = "SETTINGS.HOTKEYS_ROOT";

    public static void RegisterDefaults()
    {
        var root = ModSettingsRegistry.EnsureCategory(
            "hotkeys", "Hotkey Announcements", RootLocKey);

        int sort = 0;
        foreach (var entry in Entries)
        {
            try { RegisterEntry(root, entry, sort++); }
            catch (Exception e)
            {
                Log.Error($"[AccessibilityMod] Hotkey announcement registration failed for {entry.HotkeyKey}: {e.Message}");
            }
        }
    }

    private static void RegisterEntry(CategorySetting root, Entry entry, int sort)
    {
        // Every hotkey gets a category so the user can see it listed with its
        // binding; configurable ones (HP, Resources) also get their option
        // overrides inside.
        var category = ModSettingsRegistry.EnsureCategory(
            $"hotkeys.{entry.HotkeyKey}",
            $"Hotkey Announcements/{entry.HotkeyKey}",
            $"{RootLocKey}/{entry.LabelLocKey}");
        category.SortPriority = sort;

        // Live label: "<action label>: <binding>" reusing the same binding
        // display the keybindings settings rows show, computed each read so it
        // tracks rebinding and language changes. Ties this entry visibly to
        // its keybinding (so "Block" reads as "Announce Block: Ctrl+B").
        var action = Input.InputManager.FindAction(entry.HotkeyKey);
        if (action != null)
        {
            category.LabelProvider = () =>
                Message.Localized("ui", "SETTINGS.HOTKEYS.LABEL_WITH_BINDING",
                    new { label = action.Label, binding = action.BindingsDisplay }).Resolve();
        }

        var annKey = AnnouncementRegistry.DeriveAnnouncementKey(entry.AnnouncementType);
        var globalCategory = ModSettings.GetSetting<CategorySetting>($"announcements.{annKey}");
        if (globalCategory == null) return;

        // Mirror only the announcement's real options; skip enabled /
        // include_suffix. Optionless hotkeys keep an empty category — the
        // binding-augmented label is the information.
        foreach (var option in globalCategory.Children.Where(c => !NonOptionKeys.Contains(c.Key)))
        {
            if (category.GetByKey(option.Key) != null) continue;
            var ov = AnnouncementRegistry.CreateOverride(option);
            if (ov != null) category.Add(ov);
        }
    }

    /// <summary>
    /// Render <paramref name="announcement"/> for the given hotkey and speak
    /// it. Joins multi-line announcements into one utterance, and substitutes
    /// the hotkey's "nothing" message (e.g. "No powers") when the result is
    /// empty.
    /// </summary>
    public static void Announce(string hotkeyKey, Announcement announcement)
    {
        var sep = ByKey.TryGetValue(hotkeyKey, out var entry) ? entry.Separator : ", ";
        var ctx = AnnouncementContext.ForHotkey(hotkeyKey);
        var msg = announcement.RenderJoined(ctx, sep);

        if (string.IsNullOrEmpty(msg.Resolve()))
        {
            if (entry.EmptyLocKey == null) return;
            msg = Message.Localized("ui", entry.EmptyLocKey);
        }

        SpeechManager.Output(msg);
    }
}
