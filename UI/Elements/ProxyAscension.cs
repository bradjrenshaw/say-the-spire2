using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
using SayTheSpire2.Views;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// The in-combat top-bar ascension indicator (<c>NTopBarPortraitTip</c>).
/// The game shows just the number; on hover it lists every active ascension
/// modifier. Focus reads "Ascension N"; the UI buffer lists each modifier
/// ("Swarming Elites: Elites spawn more often." etc.) so the user can browse
/// the same detail the sighted hover tip shows.
///
/// On the November beta the tip opens with a "Game Mode: …" line, which the
/// buffer includes. In a Dread run the indicator is the Dread icon with the
/// win streak on it (no number at zero) and the tip is the mode line, the
/// streak and the four Dread rules — no modifier list; focus reads "Dread,
/// Streak N" and the buffer follows the tip.
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

    public override Message? GetLabel()
    {
        if (DreadRunView.FromRunState(GetRunState()) is { } dread)
        {
            var mode = Message.Raw(dread.ModeTitle);
            return dread.StreakText is { } streak ? Message.Join(", ", mode, Message.Raw(streak)) : mode;
        }
        return Message.Localized("ui", "ASCENSION.LABEL", new { level = GetAscensionLevel() });
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();

            var runState = GetRunState();
            int level = runState?.AscensionLevel ?? 0;
            var modeLine = runState != null ? AscensionText.GameModeLine(runState) : null;

            if (DreadRunView.FromRunState(runState) is { } dread)
            {
                // Dread's tip: mode, streak, rules. No ascension number or
                // modifier list (rule 1 says they are all on).
                uiBuffer.Add(modeLine ?? dread.ModeTitle);
                if (dread.StreakText is { } streak)
                    uiBuffer.Add(streak);
                foreach (var rule in dread.Rules)
                    uiBuffer.Add(rule);
            }
            else
            {
                uiBuffer.Add(Message.Localized("ui", "ASCENSION.LABEL", new { level }).Resolve());
                if (modeLine != null)
                    uiBuffer.Add(modeLine);

                for (int i = 1; runState != null && i <= level; i++)
                {
                    var title = AscensionText.Title(i, runState);
                    var description = AscensionText.Description(i, runState);
                    uiBuffer.Add(
                        Message.Localized("ui", "ASCENSION.MODIFIER", new { title, description }).Resolve());
                }
            }

            buffers.EnableBuffer("ui", true);
        }
        return "ui";
    }

    private static int GetAscensionLevel() => GetRunState()?.AscensionLevel ?? 0;

    private static RunState? GetRunState()
    {
        var run = NRun.Instance;
        if (run == null) return null;
        return RunStateField.GetValue(run) as RunState;
    }
}
