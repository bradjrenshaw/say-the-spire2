using System;
using System.Collections.Generic;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

public abstract class UIElement
{
    public Container? Parent { get; set; }

    public virtual bool IsVisible => true;

    /// <summary>
    /// The type whose [AnnouncementOrder] drives focus-string composition for this
    /// element. Defaults to the element's own type. Composite proxies override this
    /// to delegate to the type of whatever inner element they're wrapping so the
    /// inner's ordering (including its insertion points) governs the composed output.
    /// </summary>
    public virtual Type AnnouncementOrderType => GetType();

    // Legacy data accessors — no longer drive focus-string composition (the
    // announcement pipeline does), but non-focus callers still use them:
    //   GetLabel: HandleBuffers default, HelpScreen, StatsGameScreen, etc.
    //   GetTypeKey: FocusContext.ShouldAnnouncePosition checks
    //   GetStatusString: reactive announcements on checkbox/slider toggle
    //   GetTooltip: HandleBuffers default
    //   GetExtrasString, GetSubtypeKey: CreatureIntentFormatter.CardSummary
    public abstract Message? GetLabel();
    public virtual Message? GetExtrasString() => null;
    public virtual string? GetTypeKey() => null;
    public virtual string? GetSubtypeKey() => null;
    public virtual Message? GetStatusString() => null;
    public virtual Message? GetTooltip() => null;

    /// <summary>
    /// Fires during focus-message composition so external code can inject extra
    /// announcements without subclassing the element. Handlers append to the list.
    /// The composer positions injected announcements via [AnnouncementOrder] just
    /// like directly-yielded ones.
    /// </summary>
    public event Action<List<Announcement>>? CollectAnnouncements;

    /// <summary>
    /// Called when this element receives focus. Configure which buffers are enabled
    /// and populate them with data. Return the key of the buffer to set as current,
    /// or null to keep the default "ui" buffer.
    /// </summary>
    public virtual string? HandleBuffers(BufferManager buffers)
    {
        // Default: populate the UI buffer with label and status
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            var label = GetLabel()?.Resolve();
            if (!string.IsNullOrEmpty(label))
                uiBuffer.Add(label);
            var status = GetStatusString()?.Resolve();
            if (!string.IsNullOrEmpty(status))
                uiBuffer.Add(status);
            var tooltip = GetTooltip()?.Resolve();
            if (!string.IsNullOrEmpty(tooltip))
                uiBuffer.Add(tooltip);
            buffers.EnableBuffer("ui", true);
        }
        return "ui";
    }

    public bool IsFocused { get; private set; }

    public void Focus()
    {
        IsFocused = true;
        OnFocus();
    }

    public void Unfocus()
    {
        IsFocused = false;
        OnUnfocus();
    }

    public virtual void Update()
    {
        OnUpdate();
    }

    protected virtual void OnFocus() { }
    protected virtual void OnUnfocus() { }
    protected virtual void OnUpdate() { }

    /// <summary>
    /// Builds the spoken focus message by composing the announcements yielded by
    /// GetFocusAnnouncements plus any injected by CollectAnnouncements subscribers.
    /// </summary>
    public Message GetFocusMessage()
    {
        var announcements = new List<Announcement>(GetFocusAnnouncements());
        CollectAnnouncements?.Invoke(announcements);
        return AnnouncementComposer.Compose(this, announcements);
    }

    /// <summary>
    /// Yields the announcements that make up this element's focus message. Every
    /// concrete UIElement declares its own set; there is no default implementation
    /// beyond a potential label fallback for containers / other elements that are
    /// rarely focused directly.
    /// </summary>
    public abstract IEnumerable<Announcement> GetFocusAnnouncements();

    /// <summary>Back-compat: resolve the focus message to a string.</summary>
    public string GetFocusString() => GetFocusMessage().Resolve();
}
