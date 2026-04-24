using System.Collections.Generic;
using Godot;
using SayTheSpire2.Localization;
using SayTheSpire2.Settings;
using SayTheSpire2.Speech;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// Renders a NullableBoolSetting as a regular-looking checkbox: shows the
/// resolved value (explicit override or the inherited global), and any user
/// toggle writes an explicit value. The "inherit" state is never visible in
/// the UI — users get back to it via the category-level reset button.
/// </summary>
public class NullableCheckboxElement : UIElement
{
    // Delegate focus-string composition to CheckboxElement's [AnnouncementOrder]
    // and settings path. Nullable variants are an implementation detail for the
    // per-element override UI — to the end user, this is just a checkbox.
    public override System.Type AnnouncementOrderType => typeof(ProxyCheckbox);

    private readonly CheckBox _control;
    private readonly NullableBoolSetting _setting;
    private readonly System.Action<bool> _onResolvedChanged;

    public Node Node => _control;

    public NullableCheckboxElement(NullableBoolSetting setting)
    {
        _setting = setting;
        _control = new CheckBox
        {
            Text = setting.Label,
            ButtonPressed = setting.Resolved,
            FocusMode = Control.FocusModeEnum.None,
        };

        // Keep UI in sync if the resolved value changes (global flip while inheriting,
        // or a reset action clearing the override). Guard with IsInstanceValid so
        // a stale subscription that survives past the screen pop — e.g., during
        // the frame the Godot node is queued for free — doesn't throw.
        _onResolvedChanged = v =>
        {
            if (GodotObject.IsInstanceValid(_control))
                _control.SetPressedNoSignal(v);
        };
        setting.ResolvedChanged += _onResolvedChanged;
    }

    public override void Detach()
    {
        _setting.ResolvedChanged -= _onResolvedChanged;
        base.Detach();
    }

    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        yield return new LabelAnnouncement(_setting.Label);
        yield return new TypeAnnouncement("checkbox");
        yield return new StatusAnnouncement(
            _setting.Resolved
                ? Message.Localized("ui", "CHECKBOX.CHECKED")
                : Message.Localized("ui", "CHECKBOX.UNCHECKED"));
    }

    public override Message? GetLabel() => Message.Raw(_setting.Label);
    public override string? GetTypeKey() => "checkbox";
    public override Message? GetStatusString() =>
        _setting.Resolved
            ? Message.Localized("ui", "CHECKBOX.CHECKED")
            : Message.Localized("ui", "CHECKBOX.UNCHECKED");

    public void Activate()
    {
        var newValue = !_setting.Resolved;
        _setting.SetExplicit(newValue);
        // ResolvedChanged handler already updated _control's pressed state.
        SpeakState(newValue);
    }

    /// <summary>Called from mouse click. Godot already flipped the checkbox; write the explicit value.</summary>
    public void SyncFromControl()
    {
        var newValue = _control.ButtonPressed;
        _setting.SetExplicit(newValue);
        SpeakState(newValue);
    }

    private static void SpeakState(bool isChecked)
    {
        SpeechManager.Output(isChecked
            ? Message.Localized("ui", "CHECKBOX.CHECKED")
            : Message.Localized("ui", "CHECKBOX.UNCHECKED"));
    }
}
