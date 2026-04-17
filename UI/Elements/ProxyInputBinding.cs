using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;

namespace SayTheSpire2.UI.Elements;

[AnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(TypeAnnouncement),
    typeof(ControlValueAnnouncement)
)]
public class ProxyInputBinding : ProxyElement
{
    public override IEnumerable<Announcement> GetFocusAnnouncements()
    {
        var label = GetLabel();
        if (label != null)
            yield return new LabelAnnouncement(label);

        yield return new TypeAnnouncement("keybind");

        var status = GetStatusString();
        if (status != null)
            yield return new ControlValueAnnouncement(status);
    }

    private static readonly FieldInfo ControllerInputMapField =
        AccessTools.Field(typeof(NInputManager), "_controllerInputMap")!;

    private static readonly Dictionary<string, string> ControllerButtonNames = new()
    {
        { "controller_face_button_south", "A" },
        { "controller_face_button_east", "B" },
        { "controller_face_button_west", "X" },
        { "controller_face_button_north", "Y" },
        { "controller_left_bumper", "left bumper" },
        { "controller_right_bumper", "right bumper" },
        { "controller_left_trigger", "left trigger" },
        { "controller_right_trigger", "right trigger" },
        { "controller_d_pad_north", "D-pad up" },
        { "controller_d_pad_south", "D-pad down" },
        { "controller_d_pad_east", "D-pad right" },
        { "controller_d_pad_west", "D-pad left" },
        { "controller_start_button", "start" },
        { "controller_select_button", "select" },
        { "controller_joystick_press", "joystick press" },
        { "controller_joystick_left", "joystick left" },
        { "controller_joystick_right", "joystick right" },
        { "controller_joystick_up", "joystick up" },
        { "controller_joystick_down", "joystick down" },
    };

    public ProxyInputBinding(Control control) : base(control) { }

    public override Message? GetLabel()
    {
        var labelNode = Control?.GetNodeOrNull("%InputLabel");
        if (labelNode != null)
        {
            var text = FindChildText(labelNode);
            if (text != null) return Message.Raw(text);
        }

        var fallback = OverrideLabel ?? (Control != null ? CleanNodeName(Control.Name) : null);
        return fallback != null ? Message.Raw(fallback) : null;
    }

    public override string? GetTypeKey() => "keybind";

    public override Message? GetStatusString()
    {
        var entry = Control as NInputSettingsEntry;
        var inputName = entry?.InputName;
        bool isKeyboardRemappable = inputName != null && NInputManager.remappableKeyboardInputs.Contains(inputName);
        bool isControllerRemappable = inputName != null && NInputManager.remappableControllerInputs.Contains(inputName);

        var parts = new List<string>();

        // Keyboard binding
        if (isKeyboardRemappable)
        {
            var keyLabel = Control?.GetNodeOrNull("%KeyBindingInputLabel");
            var text = keyLabel != null ? FindChildText(keyLabel) : null;
            parts.Add(!string.IsNullOrEmpty(text) ? $"keyboard {text}" : "keyboard unbound");
        }

        // Controller binding
        if (isControllerRemappable)
        {
            var controllerName = GetControllerBindingName();
            parts.Add(controllerName != null ? $"controller {controllerName}" : "controller unbound");
        }

        // Label as keyboard-only or controller-only if applicable
        if (isKeyboardRemappable && !isControllerRemappable)
            parts.Add("keyboard only");
        else if (!isKeyboardRemappable && isControllerRemappable)
            parts.Add("controller only");

        return parts.Count > 0 ? Message.Raw(string.Join(", ", parts)) : null;
    }

    public static string GetControllerButtonName(string actionStr)
    {
        if (ControllerButtonNames.TryGetValue(actionStr, out var friendlyName))
            return friendlyName;
        return actionStr;
    }

    private string? GetControllerBindingName()
    {
        if (Control is not NInputSettingsEntry entry) return null;

        var inputName = entry.InputName;
        if (inputName == null) return null;
        if (!NInputManager.remappableControllerInputs.Contains(inputName)) return null;

        var manager = NInputManager.Instance;
        if (manager == null) return null;

        var map = ControllerInputMapField?.GetValue(manager) as Dictionary<StringName, StringName>;
        if (map == null || !map.TryGetValue(inputName, out var controllerAction)) return null;

        return GetControllerButtonName(controllerAction.ToString());
    }
}
