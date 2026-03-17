using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

namespace SayTheSpire2.UI.Elements;

public class ProxyEpochSlot : ProxyElement
{
    public ProxyEpochSlot(Control control) : base(control) { }

    public override string? GetLabel()
    {
        var slot = Control as NEpochSlot;
        if (slot?.model == null) return CleanNodeName(Control.Name);
        return slot.model.Title.GetFormattedText();
    }

    public override string? GetTypeKey() => "button";

    public override string? GetStatusString()
    {
        var slot = Control as NEpochSlot;
        if (slot?.model == null) return null;

        return slot.State switch
        {
            EpochSlotState.NotObtained => "locked",
            EpochSlotState.Obtained => "ready to reveal",
            EpochSlotState.Complete => "revealed",
            _ => null
        };
    }

    public override string? GetTooltip()
    {
        var slot = Control as NEpochSlot;
        if (slot?.model == null) return null;

        try
        {
            var unlockInfo = slot.model.UnlockInfo;
            unlockInfo.Add("IsRevealed", slot.State == EpochSlotState.Complete);
            var unlockText = StripBbcode(unlockInfo.GetFormattedText());
            if (!string.IsNullOrEmpty(unlockText))
                return unlockText;
        }
        catch (Exception e) { Log.Error($"[AccessibilityMod] Epoch slot unlock info failed: {e.Message}"); }

        return null;
    }
}
