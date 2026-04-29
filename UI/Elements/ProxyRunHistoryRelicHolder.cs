using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Relics;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

public class ProxyRunHistoryRelicHolder : ProxyRelicHolder
{
    private readonly NRelicBasicHolder? _holder;

    public ProxyRunHistoryRelicHolder(Control control) : base(control)
    {
        _holder = control as NRelicBasicHolder;
    }

    protected override IEnumerable<string> GetBufferExtraLines(RelicView view)
    {
        var line = RunHistoryAcquisitionText.FromFloor(_holder?.Relic?.Model.FloorAddedToDeck ?? view.Model.FloorAddedToDeck);
        if (line != null)
            yield return line;
    }
}
