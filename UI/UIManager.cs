using Godot;
using MegaCrit.Sts2.Core.Logging;
using Sts2AccessibilityMod.Buffers;
using Sts2AccessibilityMod.Speech;

namespace Sts2AccessibilityMod.UI;

public static class UIManager
{
    private static Control? _pendingControl;
    private static UIElement? _pendingElement;
    private static Control? _lastAnnouncedControl;
    private static bool _processingScheduled;

    /// <summary>
    /// Queue a focus change. Only the last one queued per frame will be announced.
    /// </summary>
    public static void QueueFocus(Control control, UIElement? preResolved = null)
    {
        _pendingControl = control;
        _pendingElement = preResolved;

        if (!_processingScheduled)
        {
            _processingScheduled = true;
            Callable.From(ProcessPending).CallDeferred();
        }
    }

    /// <summary>
    /// Clear the last announced tracking so the same control can be re-announced.
    /// </summary>
    public static void ClearLastAnnounced()
    {
        _lastAnnouncedControl = null;
    }

    private static void ProcessPending()
    {
        _processingScheduled = false;

        if (_pendingControl == null) return;

        var control = _pendingControl;
        var element = _pendingElement;
        _pendingControl = null;
        _pendingElement = null;

        // Skip if same control as last announced
        if (control == _lastAnnouncedControl) return;
        _lastAnnouncedControl = control;

        if (!GodotObject.IsInstanceValid(control)) return;

        element ??= ResolveElement(control);
        var text = element.GetFocusString();
        Log.Info($"[AccessibilityMod] Focus: {control.GetType().Name} ({control.Name}) -> \"{text}\"");
        if (!string.IsNullOrEmpty(text))
        {
            SpeechManager.Output(text);
        }

        var buffers = BufferManager.Instance;
        buffers.DisableAll();
        var currentBufferKey = element.HandleBuffers(buffers);
        if (currentBufferKey != null)
            buffers.SetCurrentBuffer(currentBufferKey);
    }

    private static UIElement ResolveElement(Control control)
    {
        var screenElement = GameScreenManager.ActiveScreen?.GetElement(control);
        if (screenElement != null)
            return screenElement;

        return ProxyFactory.Create(control);
    }
}
