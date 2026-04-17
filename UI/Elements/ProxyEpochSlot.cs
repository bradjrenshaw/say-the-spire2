using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement),
    typeof(TooltipAnnouncement)
)]
public class ProxyEpochSlot : ProxyElement
{
    public ProxyEpochSlot(Control control) : base(control) { }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("button");

        var status = GetStatusString();
        if (status != null)
            yield return new ControlValueAnnouncement(status);

        var tooltip = GetTooltip();
        if (tooltip != null)
            yield return new TooltipAnnouncement(tooltip);
    }

    public override Message? GetLabel()
    {
        if (Control is not NEpochSlot slot || slot.model == null)
            return Control != null ? Message.Raw(CleanNodeName(Control.Name)) : null;
        return Message.Raw(slot.model.Title.GetFormattedText());
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        var slot = Control as NEpochSlot;
        if (slot?.model == null) return null;

        var text = slot.State switch
        {
            EpochSlotState.NotObtained => LocalizationManager.GetOrDefault("ui", "TIMELINE.LOCKED", "locked"),
            EpochSlotState.Obtained => LocalizationManager.GetOrDefault("ui", "TIMELINE.READY_TO_REVEAL", "ready to reveal"),
            EpochSlotState.Complete => LocalizationManager.GetOrDefault("ui", "TIMELINE.REVEALED", "revealed"),
            _ => (string?)null
        };
        return text != null ? Message.Raw(text) : null;
    }

    public override Message? GetTooltip()
    {
        var slot = Control as NEpochSlot;
        if (slot?.model == null) return null;

        try
        {
            var unlockInfo = slot.model.UnlockInfo;
            unlockInfo.Add("IsRevealed", slot.State == EpochSlotState.Complete);
            var unlockText = StripBbcode(unlockInfo.GetFormattedText());
            if (!string.IsNullOrEmpty(unlockText))
                return Message.Raw(unlockText);
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Epoch slot unlock info failed: {e.Message}"); }

        return null;
    }
}
