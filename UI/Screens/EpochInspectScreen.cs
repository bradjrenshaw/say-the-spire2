using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Timeline;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;

namespace SayTheSpire2.UI.Screens;

public class EpochInspectScreen : GameScreen
{
    public static EpochInspectScreen? Current { get; private set; }

    public override string? ScreenName => null; // Announced via OnOpen instead

    protected override void BuildRegistry()
    {
    }

    public override void OnPush()
    {
        base.OnPush();
        Current = this;
    }

    public override void OnPop()
    {
        base.OnPop();
        if (Current == this) Current = null;
    }

    public void OnOpen(EpochModel epoch, bool wasRevealed)
    {
        try
        {
            var parts = new List<string>();

            AddEpochHeader(parts, epoch);

            if (wasRevealed)
                parts.Add("revealed");

            var desc = epoch.Description;
            if (!string.IsNullOrEmpty(desc))
                parts.Add(Message.StripBbcode(desc));

            try
            {
                var unlockText = epoch.UnlockText;
                if (!string.IsNullOrEmpty(unlockText))
                    parts.Add(Message.StripBbcode(unlockText));
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Epoch unlock text access failed: {e.Message}"); }

            if (parts.Count > 0)
            {
                var message = string.Join(". ", parts);
                Log.Info($"[AccessibilityMod] Epoch inspect: {message}");
                SpeechManager.Output(Message.Raw(message));
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Epoch inspect error: {ex.Message}");
        }
    }

    public void OnPaginate(EpochModel epoch)
    {
        try
        {
            var parts = new List<string>();

            AddEpochHeader(parts, epoch);

            var desc = epoch.Description;
            if (!string.IsNullOrEmpty(desc))
                parts.Add(Message.StripBbcode(desc));

            try
            {
                var unlockText = epoch.UnlockText;
                if (!string.IsNullOrEmpty(unlockText))
                    parts.Add(Message.StripBbcode(unlockText));
            }
            catch (Exception e) { Log.Error($"[AccessibilityMod] Epoch paginate unlock text access failed: {e.Message}"); }

            if (parts.Count > 0)
            {
                var message = string.Join(". ", parts);
                Log.Info($"[AccessibilityMod] Epoch paginate: {message}");
                SpeechManager.Output(Message.Raw(message));
            }
        }
        catch (System.Exception ex)
        {
            Log.Error($"[AccessibilityMod] Epoch paginate error: {ex.Message}");
        }
    }

    private static void AddEpochHeader(List<string> parts, EpochModel epoch)
    {
        var storyTitle = epoch.StoryTitle;
        var title = epoch.Title.GetFormattedText();
        if (!string.IsNullOrEmpty(storyTitle))
        {
            var chapterIndex = epoch.ChapterIndex;
            parts.Add($"Chapter {chapterIndex} - {title}");
            parts.Add(storyTitle);
        }
        else if (!string.IsNullOrEmpty(title))
        {
            parts.Add(title);
        }
    }
}
