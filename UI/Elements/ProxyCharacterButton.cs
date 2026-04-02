using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using SayTheSpire2.Buffers;

namespace SayTheSpire2.UI.Elements;

public class ProxyCharacterButton : ProxyElement
{
    public ProxyCharacterButton(Control control) : base(control) { }

    private NCharacterSelectButton? Button => Control as NCharacterSelectButton;

    public override string? GetLabel()
    {
        var button = Button;
        if (button == null) return CleanNodeName(Control.Name);

        if (button.IsRandom) return "Random";

        var character = button.Character;
        if (character == null) return CleanNodeName(Control.Name);

        if (button.IsLocked)
            return new LocString("main_menu_ui", "CHARACTER_SELECT.locked.title").GetFormattedText();

        return new LocString("characters", character.CharacterSelectTitle).GetFormattedText();
    }

    public override string? GetTypeKey() => null;

    public override string? GetStatusString()
    {
        var button = Button;
        if (button == null) return null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
            return "Locked";

        if (button.IsRandom) return null;

        var status = $"{character.StartingHp} HP, {character.StartingGold} gold";

        var remoteCount = button.RemoteSelectedPlayers.Count;
        if (remoteCount > 0)
            status += $", Selected by {remoteCount} other {(remoteCount == 1 ? "player" : "players")}";

        return status;
    }

    public override string? GetTooltip()
    {
        var button = Button;
        if (button == null) return null;

        var character = button.Character;
        if (character == null) return null;

        if (button.IsLocked)
        {
            var unlockText = character.GetUnlockText().GetFormattedText();
            return !string.IsNullOrEmpty(unlockText) ? unlockText : null;
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

        return parts.Count > 0 ? string.Join(". ", parts) : null;
    }

    private static string? GetAscensionText(NCharacterSelectButton button)
    {
        Node? node = button;
        while (node != null && node is not NCharacterSelectScreen)
            node = node.GetParent();
        var panel = (node as NCharacterSelectScreen)?.GetNodeOrNull<NAscensionPanel>("%AscensionPanel");
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
