using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
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
    /// Announce focus for a UIElement that owns its own control (no game Control needed).
    /// </summary>
    public static void QueueFocus(UIElement element)
    {
        _lastAnnouncedElement?.Unfocus();
        _lastAnnouncedElement = element;
        _lastAnnouncedControl = null;

        var text = BuildFocusAnnouncement(element);
        Log.Info($"[AccessibilityMod] Focus (element): {element.GetType().Name} -> \"{text}\"");
        if (!string.IsNullOrEmpty(text))
            SpeechManager.Output(Message.Raw(text));

        var buffers = BufferManager.Instance;
        buffers.ResetToAlwaysEnabled(ScreenManager.GetAlwaysEnabledBuffers());
        var currentBufferKey = element.HandleBuffers(buffers);
        if (currentBufferKey != null)
            buffers.SetCurrentBuffer(currentBufferKey);

        element.Focus();
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

        // Suppress focus announcements during end-of-turn transitions
        // (cards being removed/discarded cause erratic focus jumps)
        var cm = CombatManager.Instance;
        if (cm != null && (cm.EndingPlayerTurnPhaseOne || cm.EndingPlayerTurnPhaseTwo)) return;

        _lastAnnouncedElement?.Unfocus();

        element ??= ResolveElement(control);
        _lastAnnouncedElement = element;

        var text = BuildFocusAnnouncement(element);
        Log.Info($"[AccessibilityMod] Focus: {control.GetType().Name} ({control.Name}) -> \"{text}\"");
        if (!string.IsNullOrEmpty(text))
        {
            SpeechManager.Output(Message.Raw(text));
        }

        var buffers = BufferManager.Instance;
        buffers.ResetToAlwaysEnabled(ScreenManager.GetAlwaysEnabledBuffers());
        var currentBufferKey = element.HandleBuffers(buffers);
        if (currentBufferKey != null)
            buffers.SetCurrentBuffer(currentBufferKey);

        element.Focus();
    }

    private static string BuildFocusAnnouncement(UIElement element)
    {
        // If the element is in a container hierarchy, use path diffing
        if (element.Parent != null)
        {
            var screen = ScreenManager.CurrentScreen;
            var focusContext = screen?.FocusContext;
            if (focusContext != null)
            {
                var announcement = focusContext.BuildAnnouncement(element);
                if (!string.IsNullOrEmpty(announcement))
                    return announcement;
            }
        }

        // Fall back to the element's own focus string
        return element.GetFocusString();
    }

    private static UIElement ResolveElement(Control control)
    {
        var screenElement = ScreenManager.ResolveElement(control);
        if (screenElement != null)
            return screenElement;

        return ProxyFactory.Create(control);
    }
}
