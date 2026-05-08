using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;
using MegaTextLabel = MegaCrit.Sts2.addons.mega_text.MegaLabel;
using MegaTextRichLabel = MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Proxy for a single row in the bestiary sidebar. Reads its label, room-type
/// qualifier, and lock state from <see cref="BestiaryEntryView"/>; pulls the
/// detail panel's epithet + description from <see cref="NBestiary"/> for the
/// tooltip announcement so the user gets the same information a sighted player
/// sees on the right of the screen.
/// </summary>
[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(StatusAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyBestiaryEntry : ProxyElement
{
    private static readonly System.Reflection.FieldInfo EpithetField =
        AccessTools.Field(typeof(NBestiary), "_epithet")!;
    private static readonly System.Reflection.FieldInfo DescriptionLabelField =
        AccessTools.Field(typeof(NBestiary), "_descriptionLabel")!;

    public ProxyBestiaryEntry(Control control) : base(control) { }

    private BestiaryEntryView? View => BestiaryEntryView.FromControl(Control);

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var view = View;
        if (view == null)
        {
            if (Control != null)
                yield return new LabelAnnouncement(CleanNodeName(Control.Name));
            yield break;
        }

        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement(view.TypeKey);

        var status = GetStatusString();
        if (status != null)
            yield return new StatusAnnouncement(status);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        var view = View;
        if (view == null)
            return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;

        if (view.IsUnknown)
            return Message.Localized("ui", "LABELS.LOCKED");
        if (view.IsUnderConstruction)
            return Message.Raw(view.UnderConstructionName);
        return Message.Raw(view.MonsterTitle);
    }

    public override string? GetTypeKey() => View?.TypeKey;

    public override Message? GetStatusString()
    {
        var view = View;
        if (view == null) return null;
        if (view.IsUnknown)
            return Message.Localized("ui", "BESTIARY.LOCKED");
        if (view.IsUnderConstruction)
            return Message.Localized("ui", "BESTIARY.UNDER_CONSTRUCTION");
        return null;
    }

    /// <summary>
    /// Combines the detail panel's epithet and description (the visible content
    /// on the right of the bestiary screen) into a single tooltip. Both texts
    /// reflect whatever the game most recently rendered for the focused entry.
    /// </summary>
    public override Message? GetTooltip()
    {
        var (epithet, description) = ReadDetailLabels();

        var parts = new List<Message>();
        if (!string.IsNullOrWhiteSpace(epithet))
            parts.Add(Message.Raw(epithet));
        if (!string.IsNullOrWhiteSpace(description))
            parts.Add(Message.Raw(description));

        if (parts.Count == 0) return null;
        return Message.Join(", ", parts.ToArray());
    }

    /// <summary>
    /// Populates the UI buffer with one item per piece of bestiary info so the
    /// user can navigate them with the buffer-review controls:
    /// <c>1)</c> name, <c>2)</c> room type and locked/under-construction status,
    /// <c>3)</c> epithet, <c>4)</c> description.
    /// </summary>
    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null) return base.HandleBuffers(buffers);

        uiBuffer.Clear();

        var label = GetLabel()?.Resolve();
        if (!string.IsNullOrEmpty(label))
            uiBuffer.Add(label);

        var view = View;
        if (view != null)
        {
            var typeText = LocalizationManager.GetOrDefault(
                "ui", $"TYPES.{view.TypeKey.ToUpperInvariant()}", view.TypeKey);
            var status = GetStatusString()?.Resolve();
            uiBuffer.Add(string.IsNullOrEmpty(status) ? typeText : $"{typeText}, {status}");
        }

        var (epithet, description) = ReadDetailLabels();
        if (!string.IsNullOrWhiteSpace(epithet))
            uiBuffer.Add(epithet);
        if (!string.IsNullOrWhiteSpace(description))
            uiBuffer.Add(description);

        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    private (string? epithet, string? description) ReadDetailLabels()
    {
        var bestiary = NBestiary.Instance;
        if (bestiary == null) return (null, null);
        return (ReadLabelText(EpithetField.GetValue(bestiary)),
                ReadLabelText(DescriptionLabelField.GetValue(bestiary)));
    }

    private static string? ReadLabelText(object? node) => node switch
    {
        MegaTextRichLabel rtl => StripBbcode(rtl.Text),
        MegaTextLabel lbl => lbl.Text,
        RichTextLabel rtl => StripBbcode(rtl.Text),
        Label lbl => lbl.Text,
        _ => null,
    };
}
