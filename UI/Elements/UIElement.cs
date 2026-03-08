using System.Text;
using SayTheSpire2.Buffers;
using SayTheSpire2.Localization;

namespace SayTheSpire2.UI.Elements;

public abstract class UIElement
{
    public Container? Parent { get; set; }

    public virtual bool IsVisible => true;

    public abstract string? GetLabel();
    public virtual string? GetExtrasString() => null;
    public virtual string? GetTypeKey() => null;
    public virtual string? GetStatusString() => null;

    /// <summary>
    /// Called when this element receives focus. Configure which buffers are enabled
    /// and populate them with data. Return the key of the buffer to set as current,
    /// or null to keep the default "ui" buffer.
    /// </summary>
    public virtual string? HandleBuffers(BufferManager buffers)
    {
        // Default: populate the UI buffer with label and status
        var uiBuffer = buffers.GetBuffer("ui");
        if (uiBuffer != null)
        {
            uiBuffer.Clear();
            var label = GetLabel();
            if (!string.IsNullOrEmpty(label))
                uiBuffer.Add(label);
            var status = GetStatusString();
            if (!string.IsNullOrEmpty(status))
                uiBuffer.Add(status);
            buffers.EnableBuffer("ui", true);
        }
        return "ui";
    }

    public bool IsFocused { get; private set; }

    public void Focus()
    {
        IsFocused = true;
        OnFocus();
    }

    public void Unfocus()
    {
        IsFocused = false;
        OnUnfocus();
    }

    public virtual void Update()
    {
        OnUpdate();
    }

    protected virtual void OnFocus() { }
    protected virtual void OnUnfocus() { }
    protected virtual void OnUpdate() { }

    public string GetFocusString()
    {
        var sb = new StringBuilder();

        var label = GetLabel();
        if (!string.IsNullOrEmpty(label))
            sb.Append(label);

        var extras = GetExtrasString();
        if (!string.IsNullOrEmpty(extras))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(extras);
        }

        var typeKey = GetTypeKey();
        if (!string.IsNullOrEmpty(typeKey))
        {
            var typeName = Message.Localized("ui", $"TYPES.{typeKey.ToUpperInvariant()}").Resolve();
            if (!string.IsNullOrEmpty(typeName))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(typeName);
            }
        }

        var status = GetStatusString();
        if (!string.IsNullOrEmpty(status))
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(status);
        }

        return sb.Length > 0 ? sb.ToString() : "";
    }
}
