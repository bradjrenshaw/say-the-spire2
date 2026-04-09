using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public class ProxyCharacterButton : ProxyElement
{
    public ProxyCharacterButton(Control control) : base(control) { }

    private NCharacterSelectButton? Button => Control as NCharacterSelectButton;

    public override Message? GetLabel()
    {
        var button = Button;
        if (button == null) return Message.Raw(CleanNodeName(Control.Name));

        if (button.IsRandom) return Message.Raw(LocalizationManager.GetOrDefault("ui", "LABELS.RANDOM", "Random"));

        var character = button.Character;
        if (character == null) return Message.Raw(CleanNodeName(Control.Name));

        if (button.IsLocked)
            return Message.Raw(new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText());

        return Message.Raw(new LocString("characters", character.CharacterSelectTitle).GetFormattedText());
    }

    public override string? GetTypeKey() => null;

    public override Message? GetStatusString()
    {
        var button = Button;
        if (button == null) return null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
            return Message.Raw(LocalizationManager.GetOrDefault("ui", "LABELS.LOCKED", "Locked"));

        if (button.IsRandom) return null;

        var status = $"{character.StartingHp} HP, {character.StartingGold} gold";

        var remoteCount = button.RemoteSelectedPlayers.Count;
        if (remoteCount > 0)
            status += $", Selected by {remoteCount} other {(remoteCount == 1 ? "player" : "players")}";

        return Message.Raw(status);
    }

    public override Message? GetTooltip()
    {
        var button = Button;
        if (button == null) return null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
        {
            var unlockText = character.GetUnlockText().GetFormattedText();
            return !string.IsNullOrEmpty(unlockText) ? Message.Raw(unlockText) : null;
        }

        var parts = new System.Collections.Generic.List<string>();

        if (button.IsRandom)
        {
            var desc = new LocString("characters", character.CharacterSelectDesc).GetFormattedText();
            if (!string.IsNullOrEmpty(desc))
                parts.Add(desc);
        }

        var ascension = GetAscensionText(button);
        if (ascension != null)
            parts.Add(ascension);

        return parts.Count > 0 ? Message.Raw(string.Join(". ", parts)) : null;
    }

    private static string? GetAscensionText(NCharacterSelectButton button)
    {
        Node? node = button;
        while (node != null && node is not NCharacterSelectScreen && node is not NCustomRunScreen)
            node = node.GetParent();
        var panel = node switch
        {
            NCharacterSelectScreen characterSelect => characterSelect.GetNodeOrNull<NAscensionPanel>("%AscensionPanel"),
            NCustomRunScreen customRun => customRun.GetNodeOrNull<NAscensionPanel>("%AscensionPanel"),
            _ => null,
        };
        if (panel != null && panel.Visible)
        {
            var asc = panel.Ascension;
            var title = AscensionHelper.GetTitle(asc).GetFormattedText();
            var description = AscensionHelper.GetDescription(asc).GetFormattedText();
            return $"Ascension {asc}: {title}. {description}";
        }
        return null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var button = Button;
        if (button == null || button.IsRandom)
            return base.HandleBuffers(buffers);

        var character = button.Character;
        if (character == null)
            return base.HandleBuffers(buffers);

        // Character buffer
        var charBuffer = buffers.GetBuffer("character") as CharacterBuffer;
        if (charBuffer != null)
        {
            charBuffer.Bind(button);
            charBuffer.Update();
            buffers.EnableBuffer("character", true);
        }

        // Relic buffer (starting relic for character select)
        var relicBuffer = buffers.GetBuffer("relic");
        if (relicBuffer != null)
        {
            relicBuffer.Clear();

            if (button.IsLocked)
            {
                relicBuffer.Add(new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.title").GetFormattedText());

                var lockedRelicDesc = new LocString("main_menu_ui", "CHARACTER_SELECT.lockedRelic.description").GetFormattedText();
                if (!string.IsNullOrEmpty(lockedRelicDesc))
                    relicBuffer.Add(StripBbcode(lockedRelicDesc));
            }
            else if (character.StartingRelics.Count > 0)
            {
                var relic = character.StartingRelics[0];
                relicBuffer.Add(relic.Title.GetFormattedText());

                var relicDesc = relic.DynamicDescription.GetFormattedText();
                if (!string.IsNullOrEmpty(relicDesc))
                    relicBuffer.Add(StripBbcode(relicDesc));
            }

            buffers.EnableBuffer("relic", true);
        }

        return "character";
    }
}
