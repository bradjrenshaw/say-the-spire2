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
        RunFocusWatchdog();

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

    // --- Focus watchdog ----------------------------------------------------
    // The game's focus bookkeeping has a one-way visibility trap: TryGrabFocus
    // can set real Godot focus on a control that is not yet visible in tree
    // (its deferred grab re-checks validity but not visibility), RefreshFocus
    // then computes IsFocused = false, and becoming visible later never
    // re-runs RefreshFocus — so the focus hooks miss the control entirely
    // (map nodes at act start in multiplayer). Watch Godot's actual focus
    // owner every frame and announce any control that gained focus without
    // the normal hooks noticing. Armed once per owner change, so logical
    // (control-less) navigation and deliberately unannounced owners don't
    // retrigger it while focus sits still.
    private static Control? _watchdogOwner;
    private static bool _watchdogArmed;
    private static int _watchdogSettledFrames;

    private static void RunFocusWatchdog()
    {
        // Godot focus only drives navigation in focus-nav (controller) mode;
        // in mouse mode announcements come from hover and the focus owner is
        // frequently stale.
        if (!Input.InputManager.IsFocusNavActive) return;

        Control? owner;
        try
        {
            owner = MegaCrit.Sts2.Core.Nodes.CommonUi.NControllerManager.Instance?.GetViewport()?.GuiGetFocusOwner();
        }
        catch (System.Exception e)
        {
            Log.Info($"[AccessibilityMod] Focus watchdog viewport read failed: {e.Message}");
            return;
        }

        if (owner == null || !GodotObject.IsInstanceValid(owner))
        {
            _watchdogOwner = null;
            _watchdogArmed = false;
            return;
        }

        if (!ReferenceEquals(owner, _watchdogOwner))
        {
            // Focus moved: arm, and give the normal focus hooks a couple of
            // frames to announce it themselves.
            _watchdogOwner = owner;
            _watchdogArmed = true;
            _watchdogSettledFrames = 0;
            return;
        }

        if (!_watchdogArmed) return;

        // The normal path caught it (announced, or pending in this frame's
        // dirty state) — stand down until focus moves again.
        if (IsFocusAlreadyHandled(owner))
        {
            _watchdogArmed = false;
            return;
        }

        // Respect the same veto the focus hooks honor.
        if (Screens.ScreenManager.CurrentScreen?.ShouldSuppressFocusAnnouncement(owner) == true)
        {
            _watchdogArmed = false;
            return;
        }

        if (++_watchdogSettledFrames < 2) return;

        _watchdogArmed = false;
        Log.Info($"[AccessibilityMod] Focus watchdog: {owner.GetType().Name} gained focus without a focus event, announcing.");
        SetFocusedControl(owner);
    }

    /// <summary>
    /// Whether the Godot focus owner is already covered by the announced or
    /// pending focus state. The focus hooks deliberately announce a wrapper
    /// node that differs from the node Godot actually focuses — NCreature's
    /// OnFocus is wired to its child Hitbox, card holders and settings
    /// sliders follow the same pattern — so a raw reference comparison would
    /// treat the hitbox as a second, unannounced focus and read the same
    /// element twice. Two controls represent the same focused element when
    /// one contains the other.
    /// </summary>
    private static bool IsFocusAlreadyHandled(Control owner)
    {
        return RepresentsSameElement(owner, _currentControl)
            || RepresentsSameElement(owner, _lastAnnouncedControl)
            || RepresentsSameElement(owner, _currentElement?.Control)
            || RepresentsSameElement(owner, _lastAnnouncedElement?.Control);
    }

    private static bool RepresentsSameElement(Control owner, Control? handled)
    {
        if (handled == null || !GodotObject.IsInstanceValid(handled)) return false;
        if (ReferenceEquals(owner, handled)) return true;
        return handled.IsAncestorOf(owner) || owner.IsAncestorOf(handled);
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
