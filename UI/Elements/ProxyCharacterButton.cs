using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(LockedAnnouncement),
    typeof(StartingHpAnnouncement),
    typeof(StartingGoldAnnouncement),
    typeof(RemoteSelectionAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyCharacterButton : ProxyElement
{
    // BaseLib custom character entries reuse the game's NCharacterSelectButton
    // scene but stuff its _character field with a placeholder (the unlock
    // source, or Ironclad), so reading Character would announce the wrong
    // character entirely. BaseLib tags such buttons with metadata and hangs
    // its entry off the game's _delegate field; read the entry's own
    // title/description instead. BaseLib is an optional third-party mod, so
    // all lookups degrade gracefully (back to placeholder behavior).
    private const string BaseLibEntryMeta = "BaseLibCustomCharacterSelectEntry";

    private static readonly System.Reflection.FieldInfo? DelegateField =
        HarmonyLib.AccessTools.Field(typeof(NCharacterSelectButton), "_delegate");

    public ProxyCharacterButton(Control control) : base(control) { }

    private NCharacterSelectButton? Button => Control as NCharacterSelectButton;

    private object? BaseLibEntry
    {
        get
        {
            var button = Button;
            if (button == null || !button.HasMeta(BaseLibEntryMeta)) return null;
            var buttonDelegate = DelegateField?.GetValue(button);
            if (buttonDelegate == null) return null;
            return HarmonyLib.AccessTools.Property(buttonDelegate.GetType(), "Entry")
                ?.GetValue(buttonDelegate);
        }
    }

    private static string? GetEntryText(object entry, string propertyName)
    {
        var text = HarmonyLib.AccessTools.Property(entry.GetType(), propertyName)
            ?.GetValue(entry) as string;
        return string.IsNullOrWhiteSpace(text) ? null : StripBbcode(text);
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        var button = Button;

        if (button != null && button.IsLocked)
        {
            yield return new LockedAnnouncement();
        }
        else if (button != null && BaseLibEntry == null
            && button.Character is { } character && !button.IsRandom)
        {
            // Real stats are unknown for BaseLib entries until the entry
            // resolves to a character, so only vanilla buttons announce them.
            yield return new StartingHpAnnouncement(character.StartingHp);
            if (LobbyShowsStartingGold(button))
                yield return new StartingGoldAnnouncement(character.StartingGold);
            var remoteCount = button.RemoteSelectedPlayers.Count;
            if (remoteCount > 0)
                yield return new RemoteSelectionAnnouncement(remoteCount);
        }

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        var button = Button;
        if (button == null) return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

        if (button.IsRandom) return Message.Localized("ui", "LABELS.RANDOM");

        if (BaseLibEntry is { } entry)
        {
            var title = GetEntryText(entry, button.IsLocked ? "LockedTitle" : "EntryTitle");
            if (title != null) return Message.Raw(title);
        }

        var character = button.Character;
        if (character == null) return Message.Raw(CleanNodeName(button.Name));

        if (button.IsLocked)
            return Message.Raw(new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText());

        return Message.Raw(new LocString("characters", character.CharacterSelectTitle).GetFormattedText());
    }

    public override string? GetTypeKey() => null;

    public override Message? GetStatusString()
    {
        var button = Button;
        if (button == null) return null;

        if (BaseLibEntry != null)
            return button.IsLocked ? Message.Localized("ui", "LABELS.LOCKED") : null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
            return Message.Localized("ui", "LABELS.LOCKED");

        if (button.IsRandom) return null;

        var parts = new System.Collections.Generic.List<Message>
        {
            Message.Localized("ui", "CHARACTER.STARTING_HP", new { amount = character.StartingHp }),
            Message.Localized("ui", "CHARACTER.STARTING_GOLD", new { amount = character.StartingGold }),
        };

        var remoteCount = button.RemoteSelectedPlayers.Count;
        if (remoteCount > 0)
        {
            var remoteKey = remoteCount == 1 ? "CHARACTER.REMOTE_SELECTION_SINGLE" : "CHARACTER.REMOTE_SELECTION_PLURAL";
            parts.Add(Message.Localized("ui", remoteKey, new { count = remoteCount }));
        }

        return Message.Join(", ", parts.ToArray());
    }

    public override Message? GetTooltip()
    {
        var button = Button;
        if (button == null) return null;

        if (BaseLibEntry is { } entry)
        {
            var description = GetEntryText(entry,
                button.IsLocked ? "LockedDescription" : "EntryDescription");
            return description != null ? Message.Raw(description) : null;
        }

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
        {
            var unlockText = character.GetUnlockText().GetFormattedText();
            return !string.IsNullOrEmpty(unlockText) ? Message.Raw(unlockText) : null;
        }

        var parts = new System.Collections.Generic.List<Message>();

        if (button.IsRandom)
        {
            var desc = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
            if (!string.IsNullOrEmpty(desc))
                parts.Add(Message.Raw(desc));
        }

        var ascension = GetAscensionText(button);
        if (ascension != null)
            parts.Add(ascension);

        return parts.Count > 0 ? Message.Join(". ", parts.ToArray()) : null;
    }

    // Path of the starting-gold display inside a lobby screen's info panel.
    private const string LobbyGoldPath = "InfoPanel/VBoxContainer/HpGoldSpacer/HpGold/Gold";

    /// <summary>
    /// Whether the lobby this button sits in shows starting gold. Character
    /// select does; the Blitz lobby has the same info panel with the gold
    /// entry hidden (Blitz has no gold). Lobbies without that panel are
    /// treated as showing it, as before.
    /// </summary>
    public static bool LobbyShowsStartingGold(NCharacterSelectButton button)
    {
        for (Node? node = button.GetParent(); node != null; node = node.GetParent())
        {
            if (node.GetNodeOrNull<Control>(LobbyGoldPath) is { } gold)
                return gold.Visible;
        }
        return true;
    }

    private static Message? GetAscensionText(NCharacterSelectButton button)
    {
        // The button is instanced into its lobby screen (character select,
        // custom run, Blitz), so walk up until an ancestor inside that
        // screen's scene can resolve the screen's ascension panel.
        NAscensionPanel? panel = null;
        for (Node? node = button.GetParent(); node != null && panel == null; node = node.GetParent())
            panel = node.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
        if (panel != null && panel.Visible)
        {
            var asc = panel.Ascension;
            var title = AscensionText.Title(asc, panel);
            var description = AscensionText.Description(asc, panel);
            return Message.Localized("ui", "CHARACTER.ASCENSION_DETAIL", new
            {
                level = asc,
                title,
                description
            });
        }
        return null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var button = Button;
        if (button == null || button.IsRandom)
            return base.HandleBuffers(buffers);

        // BaseLib entries: the placeholder character's stats and relic would
        // be wrong. The base handler fills the ui buffer from
        // GetLabel/GetStatusString/GetTooltip, which are entry-aware.
        if (BaseLibEntry != null)
            return base.HandleBuffers(buffers);

        var character = button.Character;
        if (character == null)
            return base.HandleBuffers(buffers);

        // Character buffer
        var charBuffer = buffers.GetBuffer("character") as CharacterBuffer;
        if (charBuffer != null)
        {
            charBuffer.Bind(button);
            charBuffer.Update();
            buffers.EnableBuffer("character", true);
        }

        // Relic buffer (starting relic for character select)
        var relicBuffer = buffers.GetBuffer("relic");
        if (relicBuffer != null)
        {
            relicBuffer.Clear();

            if (button.IsLocked)
            {
                relicBuffer.Add(new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.title").GetFormattedText());

                var lockedRelicDesc = new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.description").GetFormattedText();
                if (!string.IsNullOrEmpty(lockedRelicDesc))
                    relicBuffer.Add(StripBbcode(lockedRelicDesc));
            }
            else if (character.StartingRelics.Count > 0)
            {
                var relic = character.StartingRelics[0];
                relicBuffer.Add(relic.Title.GetFormattedText());

                var relicDesc = relic.DynamicDescription.GetFormattedText();
                if (!string.IsNullOrEmpty(relicDesc))
                    relicBuffer.Add(StripBbcode(relicDesc));
            }

            buffers.EnableBuffer("relic", true);
        }

        return "character";
    }
}
