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

    /// <summary>
    /// The element the mod considers focused. Truth-source across both focus
    /// modalities — Godot-driven (set via SetFocusedControl) and logical (set
    /// via SetFocusedElement from NavigableContainer-style screens). Used by
    /// ContainerNavigation so Home/End operates on the right container even
    /// when Godot's focus owner is stale.
    /// </summary>
    public static UIElement? CurrentElement => _currentElement;
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

        // A pre-resolved element whose control was freed since the focus
        // event (e.g. a hand card holder discarded at end of turn) must not
        // be announced — native reads like Control.Name would throw
        // ObjectDisposedException mid-frame.
        if (_currentElement.Control != null && !GodotObject.IsInstanceValid(_currentElement.Control))
        {
            _currentControl = null;
            _currentElement = null;
            return;
        }

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
        var message = BuildFocusAnnouncement(element);
        var text = message?.Resolve();

        // Identity first, never text: a different control or element is a
        // different UI element and always announces, even when its text is
        // identical to the previous announcement (e.g. two settings rows
        // with the same label, or virtual elements with no control). The
        // text comparison only suppresses re-announcing the SAME element
        // with unchanged content — frame noise from repeated focus events.
        var controlChanged = !ReferenceEquals(_currentControl, _lastAnnouncedControl);
        var elementChanged = !ReferenceEquals(element, _lastAnnouncedElement);
        if (string.IsNullOrEmpty(text))
            return;
        if (!controlChanged && !elementChanged && text == _lastAnnouncedText)
            return;

        // Detect focus wrapping (structurally for containered elements,
        // geometrically for fallback proxies) and play the wrap sound.
        if (controlChanged)
            Audio.SoundEffects.CheckWrap(_lastAnnouncedElement, element, _lastAnnouncedControl, _currentControl);

        _lastAnnouncedText = text;
        _lastAnnouncedControl = _currentControl;

        // Unfocus previous, focus new
        if (_lastAnnouncedElement != null && _lastAnnouncedElement != element)
            _lastAnnouncedElement.Unfocus();
        _lastAnnouncedElement = element;

        Log.Info($"[AccessibilityMod] Focus: {element.GetType().Name} -> \"{text}\"");
        SpeechManager.Output(message!);

        // Update buffers
        var buffers = BufferManager.Instance;
        buffers.ResetToAlwaysEnabled(ScreenManager.GetAlwaysEnabledBuffers());
        var currentBufferKey = element.HandleBuffers(buffers);
        if (currentBufferKey != null)
            buffers.SetCurrentBuffer(currentBufferKey);

        element.Focus();
    }

    /// <summary>
    /// Forget the last announcement so the next focus event announces even
    /// when it lands on the same element with the same text. Called on
    /// screen-stack transitions: screens that persist and reuse their
    /// controls (the game's map screen) refocus the same node on reopen,
    /// which must be announced again.
    /// </summary>
    public static void ResetAnnouncementDedupe()
    {
        _lastAnnouncedText = null;
    }

    private static Message? BuildFocusAnnouncement(UIElement element)
    {
        // If the element is in a container hierarchy, use path diffing
        if (element.Parent != null)
        {
            var announcement = _focusContext.BuildAnnouncement(element);
            if (announcement != null)
                return announcement;
        }

        // Fall back to the element's own focus message
        return element.GetFocusMessage();
    }

    private static UIElement ResolveElement(Control control)
    {
        var screenElement = ScreenManager.ResolveElement(control);
        if (screenElement != null)
            return screenElement;

        return ProxyFactory.Create(control);
    }
}
