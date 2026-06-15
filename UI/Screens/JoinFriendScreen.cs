using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.Localization;
using SayTheSpire2.Speech;

namespace SayTheSpire2.UI.Screens;

/// <summary>
/// "Choose Friend to Join" — the multiplayer friend-join lobby. Friend
/// buttons are real NJoinFriendButton controls and already get announced
/// through the normal focus path. The empty state, though, is just a
/// MegaLabel ("No friends currently playing multiplayer.") with nothing
/// focusable, so it was never read. Watch that label and speak it the
/// moment it becomes visible (the game fills the friend list asynchronously,
/// then shows the label only if no friends were found).
/// </summary>
public class JoinFriendScreen : Screen
{
    private static readonly FieldInfo? NoFriendsLabelField =
        AccessTools.Field(typeof(NJoinFriendScreen), "_noFriendsLabel");

    private readonly NJoinFriendScreen _screen;
    private bool _spokeNoFriends;

    public JoinFriendScreen(NJoinFriendScreen screen)
    {
        _screen = screen;
    }

    public override void OnUpdate()
    {
        var label = NoFriendsLabelField?.GetValue(_screen) as Control;
        bool visible = label != null && label.IsVisibleInTree();

        if (visible && !_spokeNoFriends)
        {
            _spokeNoFriends = true;
            SpeechManager.Output(
                Message.Raw(new LocString("main_menu_ui", "JOIN_FRIENDS_MENU.noFriends").GetFormattedText()));
        }
        else if (!visible)
        {
            // Reset so a later refresh that re-shows the empty state speaks again.
            _spokeNoFriends = false;
        }
    }
}
