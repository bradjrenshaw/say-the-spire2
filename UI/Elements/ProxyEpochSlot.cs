using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyEpochSlot : ProxyElement
{
    public ProxyEpochSlot(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        var slot = Control as NEpochSlot;
        if (slot?.model == null) return Message.Raw(CleanNodeName(Control.Name));
        return Message.Raw(slot.model.Title.GetFormattedText());
    }

    public override string? GetTypeKey() => "button";

    public override Message? GetStatusString()
    {
        var slot = Control as NEpochSlot;
        if (slot?.model == null) return null;

        var text = slot.State switch
        {
            EpochSlotState.NotObtained => "locked",
            EpochSlotState.Obtained => "ready to reveal",
            EpochSlotState.Complete => "revealed",
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
