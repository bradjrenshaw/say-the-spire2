using Godot;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Buffers;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.UI;

public static class UIManager
{
    private static Control? _pendingControl;
    private static UIElement? _pendingElement;
    private static Control? _lastAnnouncedControl;
    private static UIElement? _lastAnnouncedElement;
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

        _lastAnnouncedElement?.OnUnfocus();

        element ??= ResolveElement(control);
        _lastAnnouncedElement = element;

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

        element.OnFocus();
    }

    private static UIElement ResolveElement(Control control)
    {
        var screenElement = ScreenManager.ResolveElement(control);
        if (screenElement != null)
            return screenElement;

        return ProxyFactory.Create(control);
    }
}
