using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SayTheSpire2.Buffers;
using SayTheSpire2.Input;
using SayTheSpire2.Settings;

namespace SayTheSpire2.UI.Screens;

public class DefaultScreen : Screen
{
    public DefaultScreen()
    {
        ClaimAction("buffer_next_item");
        ClaimAction("buffer_prev_item");
        ClaimAction("buffer_next");
        ClaimAction("buffer_prev");
        ClaimAction("reset_bindings");
        ClaimAction("mod_settings");
    }

    public override bool OnActionJustPressed(InputAction action)
    {
        switch (action.Key)
        {
            case "buffer_next_item":
                BufferControls.NextItem();
                return true;
            case "buffer_prev_item":
                BufferControls.PreviousItem();
                return true;
            case "buffer_next":
                BufferControls.NextBuffer();
                return true;
            case "buffer_prev":
                BufferControls.PreviousBuffer();
                return true;
            case "reset_bindings":
                Log.Info("[AccessibilityMod] Global hotkey: Ctrl+Shift+R - resetting bindings");
                NInputManager.Instance?.ResetToDefaults();
                return true;
            case "mod_settings":
                OpenModSettings();
                return true;
        }

        return false;
    }

    private static void OpenModSettings()
    {
        var screen = new ModSettingsScreen(ModSettings.Root);
        ScreenManager.PushScreen(screen);
    }
}
