using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Screens.FeedbackScreen;
using SayTheSpire2.Buffers;
using SayTheSpire2.Help;
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
        ClaimAction("help");
        ClaimAction("dev_console");
        ClaimAction("feedback");
        ClaimAction("nav_home");
        ClaimAction("nav_end");
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
                Log.Info("[AccessibilityMod] Global hotkey: Ctrl+Shift+R - resetting mod bindings");
                InputManager.ResetToDefaults();
                Speech.SpeechManager.Output(Localization.Message.Localized("ui", "SPEECH.BINDINGS_RESET"));
                return true;
            case "mod_settings":
                OpenModMenu();
                return true;
            case "help":
                OpenHelpScreen();
                return true;
            case "dev_console":
                ToggleDevConsole();
                return true;
            case "feedback":
                OpenFeedbackScreen();
                return true;
            case "nav_home":
                ContainerNavigation.JumpToFirst();
                return true;
            case "nav_end":
                ContainerNavigation.JumpToLast();
                return true;
        }

        return false;
    }

    private static void OpenModMenu()
    {
        var screen = new ModMenuScreen();
        ScreenManager.PushScreen(screen);
    }

    private static void OpenHelpScreen()
    {
        var builder = new HelpScreenBuilder();
        builder.AddFromScreenStack();
        builder.AddAlwaysPresent();
        var screen = new HelpScreen(builder.Build());
        ScreenManager.PushScreen(screen);
    }

    private static void ToggleDevConsole()
    {
        try
        {
            var console = NDevConsole.Instance;
            if (console.Visible)
                console.HideConsole();
            else
                console.ShowConsole();
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Dev console toggle failed: {e.Message}");
        }
    }

    private static void OpenFeedbackScreen()
    {
        try
        {
            var opener = NFeedbackScreenOpener.Instance;
            if (opener == null) return;

            var feedbackScreen = MegaCrit.Sts2.Core.Nodes.NGame.Instance?.FeedbackScreen;
            if (feedbackScreen == null || feedbackScreen.Visible) return;

            MegaCrit.Sts2.Core.Helpers.TaskHelper.RunSafely(opener.OpenFeedbackScreen());
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Feedback screen failed: {e.Message}");
        }
    }
}
