using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using SayTheSpire2.Localization;
using SayTheSpire2.UI.Announcements;
namespace SayTheSpire2.Buffers;

[BufferAnnouncementOrder(
    typeof(LabelAnnouncement),
    typeof(LockedAnnouncement),
    typeof(DescriptionAnnouncement),
    typeof(StartingHpAnnouncement),
    typeof(StartingGoldAnnouncement),
    typeof(EnergyAnnouncement),
    typeof(DreadStreakAnnouncement)
)]
public class CharacterBuffer : Buffer
{
    private NCharacterSelectButton? _button;

    public CharacterBuffer() : base("character") { }

    public void Bind(NCharacterSelectButton button)
    {
        _button = button;
    }

    protected override void ClearBinding()
    {
        _button = null;
        Clear();
    }

    public override void Update()
    {
        if (_button == null) return;
        Repopulate(() => Populate(this, _button));
    }

    public static void Populate(Buffer buffer, NCharacterSelectButton button)
    {
        var attrOrder = typeof(CharacterBuffer).GetCustomAttributes(typeof(BufferAnnouncementOrderAttribute), inherit: true)
            is { Length: > 0 } attrs && attrs[0] is BufferAnnouncementOrderAttribute order
            ? order.Types
            : Array.Empty<Type>();

        BufferAnnouncementComposer.Compose(buffer, "character", attrOrder, BuildAnnouncements(button));
    }

    private static IEnumerable<Announcement> BuildAnnouncements(NCharacterSelectButton button)
    {
        var character = button.Character;
        if (character == null) yield break;

        if (button.IsLocked)
        {
            yield return new LabelAnnouncement(
                new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText());
            yield return new LockedAnnouncement();

            var unlockText = character.GetUnlockText().GetFormattedText();
            if (!string.IsNullOrEmpty(unlockText))
                yield return new DescriptionAnnouncement(unlockText);
            yield break;
        }

        yield return new LabelAnnouncement(
            new LocString("characters", character.CharacterSelectTitle).GetFormattedText());

        var desc = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            yield return new DescriptionAnnouncement(desc);

        // The Dread lobby shows no starting HP or gold; it shows the
        // character's win streak instead.
        var dread = Views.DreadLobbyView.FromControl(button);
        if (dread == null)
        {
            yield return new StartingHpAnnouncement(character.StartingHp);
            if (UI.Elements.ProxyCharacterButton.LobbyShowsStartingGold(button))
                yield return new StartingGoldAnnouncement(character.StartingGold);
        }
        yield return new EnergyAnnouncement(character.MaxEnergy, character.MaxEnergy);

        if (dread != null && dread.StreakLinesFor(button) is { Count: > 0 } streak)
            yield return new DreadStreakAnnouncement(streak);
    }
}
