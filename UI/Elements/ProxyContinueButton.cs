using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

/// <summary>
/// The main menu's Continue button. Focusing it shows the saved run's summary
/// panel (<c>NContinueRunInfo</c>): character icon, act and floor, the run's
/// mode / ascension line ("Ascension 5", "Dread", "Blitz"…), HP, gold (hidden
/// for Blitz) and when it was saved — or an error if the save can't be read.
/// The panel is read off its own labels, so whatever the game shows for a
/// mode is what is read.
/// </summary>
public class ProxyContinueButton : ProxyButton
{
    private readonly NContinueRunInfo _info;

    public ProxyContinueButton(Control control, NContinueRunInfo info) : base(control)
    {
        _info = info;
    }

    // Share settings / [AnnouncementOrder] with ProxyButton.
    public override Type AnnouncementOrderType => typeof(ProxyButton);

    public override Message? GetTooltip()
    {
        var lines = GetInfoLines();
        return lines.Count > 0 ? Message.Join(", ", lines.ToArray()) : null;
    }

    public override string? HandleBuffers(BufferManager buffers)
    {
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer == null) return base.HandleBuffers(buffers);

        uiBuffer.Clear();
        var label = GetLabel()?.Resolve();
        if (!string.IsNullOrEmpty(label))
            uiBuffer.Add(label);
        foreach (var line in GetInfoLines())
        {
            var text = line.Resolve();
            if (!string.IsNullOrEmpty(text))
                uiBuffer.Add(text);
        }
        buffers.EnableBuffer("ui", true);
        return "ui";
    }

    private List<Message> GetInfoLines()
    {
        var lines = new List<Message>();
        if (!GodotObject.IsInstanceValid(_info) || !_info.HasResult)
            return lines;

        // Unreadable save: the panel shows only an error message.
        if (_info.GetNodeOrNull<Control>("%ErrorContainer") is { Visible: true } error)
        {
            var errorText = FindChildText(error);
            if (!string.IsNullOrWhiteSpace(errorText))
                lines.Add(Message.Raw(errorText));
            return lines;
        }

        if (CharacterName() is { } character)
            lines.Add(Message.Raw(character));

        AddLabel(lines, "%ProgressLabel");
        AddLabel(lines, "%AscensionLabel");

        // HP and gold are bare numbers beside icons.
        if (LabelText("%HealthLabel") is { } health)
        {
            var parts = health.Split('/');
            lines.Add(parts.Length == 2 && int.TryParse(parts[0], out var current) && int.TryParse(parts[1], out var max)
                ? Message.Localized("ui", "TOPBAR.HP", new { current, max })
                : Message.Raw(health));
        }
        if (LabelText("%GoldLabel") is { } gold)
        {
            lines.Add(int.TryParse(gold, out var amount)
                ? Message.Localized("ui", "TOPBAR.GOLD", new { amount })
                : Message.Raw(gold));
        }

        AddLabel(lines, "%DateLabel");
        return lines;
    }

    // The character is shown only as an icon; name it by matching the icon.
    private string? CharacterName()
    {
        try
        {
            var path = _info.GetNodeOrNull<TextureRect>("%CharacterIcon")?.Texture?.ResourcePath;
            if (string.IsNullOrEmpty(path)) return null;
            return ModelDb.AllCharacters.FirstOrDefault(c => c.IconTexture?.ResourcePath == path)
                ?.Title.GetFormattedText();
        }
        catch (Exception e)
        {
            Log.Info($"[AccessibilityMod] Continue run character lookup failed: {e.Message}");
            return null;
        }
    }

    private void AddLabel(List<Message> lines, string path)
    {
        if (LabelText(path) is { } text)
            lines.Add(Message.Raw(text));
    }

    // Text of one of the panel's labels, or null when the game has it hidden
    // (no ascension line; no gold in Blitz) or empty.
    private string? LabelText(string path)
    {
        var label = _info.GetNodeOrNull<RichTextLabel>(path);
        if (label == null || !label.Visible || string.IsNullOrWhiteSpace(label.Text))
            return null;
        var text = StripBbcode(label.Text).Replace('\n', ' ').Trim();
        return text.Length > 0 ? text : null;
    }
}
