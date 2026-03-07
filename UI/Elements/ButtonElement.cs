using System;
using Godot;

namespace SayTheSpire2.UI.Elements;

public class ButtonElement : UIElement
{
    private readonly Button _control;
    private readonly string _label;

    public Action? OnActivated { get; set; }
    public Node Node => _control;

    public ButtonElement(string label)
    {
        _label = label;
        _control = new Button
        {
            Text = label,
            FocusMode = Control.FocusModeEnum.None,
        };
    }

    public override string? GetLabel() => _label;
    public override string? GetTypeKey() => "button";

    public void Activate()
    {
        OnActivated?.Invoke();
    }
}
