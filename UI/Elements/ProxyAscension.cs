using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// The in-combat top-bar ascension indicator (<c>NTopBarPortraitTip</c>).
/// The game shows just the number; on hover it lists every active ascension
/// modifier. Focus reads "Ascension N"; the UI buffer lists each modifier
/// ("Swarming Elites: Elites spawn more often." etc.) so the user can browse
/// the same detail the sighted hover tip shows.
/// </summary>
[AnnouncementOrder(typeof(LabelAnnouncement))]
public class ProxyAscension : ProxyElement
{
    private static readonly FieldInfo RunStateField =
        AccessTools.Field(typeof(NRun), "_state")!;

    public ProxyAscension(Control control) : base(control) { }

    public override string? GetTypeKey() => "ascension";

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);
    }

    public override Message? GetLabel() =>
        Message.Localized("ui", "ASCENSION.LABEL", new { level = GetAscensionLevel() });

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();

            int level = GetAscensionLevel();
            uiBuffer.Add(Message.Localized("ui", "ASCENSION.LABEL", new { level }).Resolve());

            for (int i = 1; i <= level; i++)
            {
                var title = AscensionHelper.GetTitle(i).GetFormattedText();
                var description = AscensionHelper.GetDescription(i).GetFormattedText();
                uiBuffer.Add(
                    Message.Localized("ui", "ASCENSION.MODIFIER", new { title, description }).Resolve());
            }

            buffers.EnableBuffer("ui", true);
        }
        return "ui";
    }

    private static int GetAscensionLevel()
    {
        var run = NRun.Instance;
        if (run == null) return 0;
        return (RunStateField.GetValue(run) as RunState)?.AscensionLevel ?? 0;
    }
}
