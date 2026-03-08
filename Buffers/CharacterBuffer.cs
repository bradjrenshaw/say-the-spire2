using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
namespace SayTheSpire2.Buffers;

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
        Repopulate(Populate);
    }

    private void Populate()
    {
        var button = _button;
        if (button == null) return;

        var character = button.Character;
        if (character == null) return;

        if (button.IsLocked)
        {
            Add(new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText());

            var unlockText = character.GetUnlockText().GetFormattedText();
            if (!string.IsNullOrEmpty(unlockText))
                Add(unlockText);
        }
        else
        {
            Add(new LocString("characters", character.CharacterSelectTitle).GetFormattedText());

            var desc = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
            if (!string.IsNullOrEmpty(desc))
                Add(desc);

            Add($"HP: {character.StartingHp}");
            Add($"Gold: {character.StartingGold}");
            Add($"Energy: {character.MaxEnergy}");
        }
    }
}
