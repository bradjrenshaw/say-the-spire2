using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.Buffers;

public static class BufferControls
{
    public static void NextBuffer()
    {
        BufferManager.Instance.MoveToNext();
        ReportCurrentBuffer();
    }

    public static void PreviousBuffer()
    {
        BufferManager.Instance.MoveToPrevious();
        ReportCurrentBuffer();
    }

    public static void NextItem()
    {
        var buffer = BufferManager.Instance.CurrentBuffer;
        buffer?.MoveToNext();
        ReportCurrentItem(buffer);
    }

    public static void PreviousItem()
    {
        var buffer = BufferManager.Instance.CurrentBuffer;
        buffer?.MoveToPrevious();
        ReportCurrentItem(buffer);
    }

    private static void ReportCurrentBuffer()
    {
        var buffer = BufferManager.Instance.CurrentBuffer;
        if (buffer == null)
        {
            var msg = LocalizationManager.Get("ui", "BUFFERS.NO_BUFFERS") ?? "No buffers available";
            SpeechManager.Output(msg);
            return;
        }

        if (buffer.IsEmpty)
        {
            var msg = LocalizationManager.Get("ui", "BUFFERS.EMPTY");
            if (msg != null)
                msg = msg.Replace("{buffer}", buffer.Label);
            else
                msg = $"{buffer.Label}: empty";
            SpeechManager.Output(msg);
            return;
        }

        var item = buffer.CurrentItem;
        var text = LocalizationManager.Get("ui", "BUFFERS.CURRENT");
        if (text != null)
            text = text.Replace("{buffer}", buffer.Label).Replace("{item}", item ?? "");
        else
            text = $"{buffer.Label}: {item}";

        Log.Info($"[AccessibilityMod] Buffer: {buffer.Key} -> \"{item}\"");
        SpeechManager.Output(text);
    }

    private static void ReportCurrentItem(Buffer? buffer)
    {
        if (buffer == null)
        {
            var msg = LocalizationManager.Get("ui", "BUFFERS.NO_BUFFER_SELECTED") ?? "No buffer selected";
            SpeechManager.Output(msg);
            return;
        }

        if (buffer.IsEmpty)
        {
            var msg = LocalizationManager.Get("ui", "BUFFERS.EMPTY");
            if (msg != null)
                msg = msg.Replace("{buffer}", buffer.Label);
            else
                msg = $"{buffer.Label}: empty";
            SpeechManager.Output(msg);
            return;
        }

        var item = buffer.CurrentItem;
        if (item != null)
        {
            Log.Info($"[AccessibilityMod] Buffer item: {buffer.Key}[{buffer.Position}] -> \"{item}\"");
            SpeechManager.Output(item);
        }
    }
}
