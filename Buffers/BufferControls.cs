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
            SpeechManager.Output(Message.Localized("ui", "BUFFERS.NO_BUFFERS"));
            return;
        }

        if (buffer.IsEmpty)
        {
            SpeechManager.Output(Message.Localized("ui", "BUFFERS.EMPTY", new { buffer = buffer.Label }));
            return;
        }

        var item = buffer.CurrentItem ?? "";
        Log.Info($"[AccessibilityMod] Buffer: {buffer.Key} -> \"{item}\"");
        SpeechManager.Output(Message.Localized("ui", "BUFFERS.CURRENT", new { buffer = buffer.Label, item }));
    }

    private static void ReportCurrentItem(Buffer? buffer)
    {
        if (buffer == null)
        {
            SpeechManager.Output(Message.Localized("ui", "BUFFERS.NO_BUFFER_SELECTED"));
            return;
        }

        if (buffer.IsEmpty)
        {
            SpeechManager.Output(Message.Localized("ui", "BUFFERS.EMPTY", new { buffer = buffer.Label }));
            return;
        }

        var item = buffer.CurrentItem;
        if (item != null)
        {
            Log.Info($"[AccessibilityMod] Buffer item: {buffer.Key}[{buffer.Position}] -> \"{item}\"");
            SpeechManager.Output(Message.Raw(item));
        }
    }
}
