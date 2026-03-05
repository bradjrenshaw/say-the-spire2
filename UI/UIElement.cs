using System.Text;
using Sts2AccessibilityMod.Localization;

namespace Sts2AccessibilityMod.UI;

public abstract class UIElement
{
    public abstract string? GetLabel();
    public virtual string? GetExtrasString() => null;
    public virtual string? GetTypeKey() => null;
    public virtual string? GetStatusString() => null;
    public virtual LocalizationString? GetPosition() => null;

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
            var typeName = LocalizationManager.Get("ui", $"TYPES.{typeKey.ToUpperInvariant()}");
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

        var position = GetPosition();
        if (position != null)
        {
            var posStr = position.ToString();
            if (!string.IsNullOrEmpty(posStr))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(posStr);
            }
        }

        return sb.Length > 0 ? sb.ToString() : "";
    }
}
