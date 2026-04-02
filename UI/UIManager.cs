using Godot;
using MegaCrit.Sts2.Core.Logging;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Elements;
using SayTheSpire2.UI.Screens;

namespace SayTheSpire2.UI;

public static class UIManager
{
    private static UIElement? _currentElement;
    private static Control? _currentControl;
    private static Control? _lastAnnouncedControl;
    private static UIElement? _lastAnnouncedElement;
    private static string? _lastAnnouncedText;
    private static bool _dirty;
    private static readonly FocusContext _focusContext = new();

    /// <summary>
    /// Set the focused element from a game Control (e.g., from focus hooks).
    /// The element will be resolved from the screen registry if not pre-resolved.
    /// Announcing happens in the Update loop, not here.
    /// </summary>
    public static void SetFocusedControl(Control control, UIElement? preResolved = null)
    {
        _currentControl = control;
        _currentElement = preResolved;
        _dirty = true;
    }

    /// <summary>
    /// Set the focused element directly (e.g., from NavigableContainer).
    /// Announcing happens in the Update loop, not here.
    /// </summary>
    public static void SetFocusedElement(UIElement element)
    {
        _currentElement = element;
        _currentControl = null;
        _dirty = true;
    }

    /// <summary>
    /// Called once per frame from ProcessPostfix. Resolves the current element,
    /// diffs the container path, and announces changes.
    /// </summary>
    public static void Update()
    {
        if (!_dirty) return;
        _dirty = false;

        // Resolve element if we have a control but no pre-resolved element
        if (_currentControl != null && _currentElement == null)
        {
            if (!GodotObject.IsInstanceValid(_currentControl))
            {
                _currentControl = null;
                return;
            }
            _currentElement = ResolveElement(_currentControl);
        }

        if (_currentElement == null) return;

        // Try to upgrade via screen registry (gives container context for path diffing).
        // Only replace the current element if the screen actually has it registered —
        // don't fall back to ProxyFactory which would produce a generic downgrade.
        var element = _currentElement;
        if (_currentControl != null && GodotObject.IsInstanceValid(_currentControl))
        {
            var screenResolved = Screens.ScreenManager.ResolveElement(_currentControl);
            if (screenResolved != null)
                element = screenResolved;
        }

        // Build announcement via path diffing
        var text = BuildFocusAnnouncement(element);

        // Only announce if something changed (text or control reference)
        var controlChanged = _currentControl != null && _currentControl != _lastAnnouncedControl;
        if (string.IsNullOrEmpty(text) || (text == _lastAnnouncedText && !controlChanged))
            return;

        _lastAnnouncedText = text;
        _lastAnnouncedControl = _currentControl;

        // Unfocus previous, focus new
        if (_lastAnnouncedElement != null && _lastAnnouncedElement != element)
            _lastAnnouncedElement.Unfocus();
        _lastAnnouncedElement = element;

        Log.Info($"[AccessibilityMod] Focus: {element.GetType().Name} -> \"{text}\"");
        SpeechManager.Output(Message.Raw(text));

        // Update buffers
        var buffers = BufferManager.Instance;
        buffers.ResetToAlwaysEnabled(ScreenManager.GetAlwaysEnabledBuffers());
        var currentBufferKey = element.HandleBuffers(buffers);
        if (currentBufferKey != null)
            buffers.SetCurrentBuffer(currentBufferKey);

        element.Focus();
    }

    private static string? BuildFocusAnnouncement(UIElement element)
    {
        // If the element is in a container hierarchy, use path diffing
        if (element.Parent != null)
        {
            var announcement = _focusContext.BuildAnnouncement(element);
            if (!string.IsNullOrEmpty(announcement))
                return announcement;
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
