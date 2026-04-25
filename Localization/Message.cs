using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SayTheSpire2.Localization;

/// <summary>
/// A speech message that carries its text (raw or localized) and resolves it
/// through a consistent pipeline: localization lookup → variable substitution → BBCode stripping.
/// Messages are composable via the + operator.
/// </summary>
public class Message
{
    /// <summary>
    /// Localization resolver function. Must be set before resolving localized messages.
    /// Typically set to LocalizationManager.Get during mod initialization.
    /// </summary>
    public static Func<string, string, string?>? LocalizationResolver { get; set; }

    private static readonly Regex ImgPattern = new(@"\[img\](.*?)\[/img\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BbcodePattern = new(@"\[.*?\]", RegexOptions.Compiled);
    private static readonly Regex ResPathPattern = new(@"res://\S+", RegexOptions.Compiled);
    private static readonly Regex VariablePattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

    // Icon suffix → ui.json loc key. Resolved each time so language switches at
    // runtime apply to card descriptions / any bbcode-stripped text.
    private static readonly Dictionary<string, string> IconNames = new()
    {
        { "energy_icon", "ICONS.ENERGY" },
        { "star_icon", "ICONS.STAR" },
        { "gold_icon", "ICONS.GOLD" },
        { "card_icon", "ICONS.CARD" },
        { "chest_icon", "ICONS.CHEST" },
    };

    private readonly string? _rawText;
    private readonly string? _table;
    private readonly string? _key;
    private readonly Dictionary<string, string>? _vars;
    private readonly List<Message>? _parts;
    private readonly string? _separator;

    private Message(string? rawText, string? table, string? key, Dictionary<string, string>? vars)
    {
        _rawText = rawText;
        _table = table;
        _key = key;
        _vars = vars;
    }

    private Message(List<Message> parts, string separator)
    {
        _parts = parts;
        _separator = separator;
    }

    /// <summary>Create a message from raw text (already localized by the game or a literal value).</summary>
    public static Message Raw(string text)
    {
        return new Message(text, null, null, null);
    }

    /// <summary>Create a message from raw text with variable substitution (anonymous object).</summary>
    public static Message Raw(string text, object vars)
    {
        return new Message(text, null, null, ObjectToDict(vars));
    }

    /// <summary>Create a message from raw text with variable substitution (dictionary).</summary>
    public static Message Raw(string text, Dictionary<string, string> vars)
    {
        return new Message(text, null, null, vars);
    }

    /// <summary>Create a message from a localization key.</summary>
    public static Message Localized(string table, string key)
    {
        return new Message(null, table, key, null);
    }

    /// <summary>Create a message from a localization key with variable substitution (anonymous object).</summary>
    public static Message Localized(string table, string key, object vars)
    {
        return new Message(null, table, key, ObjectToDict(vars));
    }

    /// <summary>Create a message from a localization key with variable substitution (dictionary).</summary>
    public static Message Localized(string table, string key, Dictionary<string, string> vars)
    {
        return new Message(null, table, key, vars);
    }

    /// <summary>Create a separator message for use between composed parts.</summary>
    public static Message Sep(string separator = ", ")
    {
        return new Message(separator, null, null, null);
    }

    /// <summary>An empty message that resolves to empty string.</summary>
    public static readonly Message Empty = new(string.Empty, null, null, null);

    /// <summary>
    /// Join multiple messages with a separator. Null/empty parts are skipped.
    /// </summary>
    public static Message Join(string separator, params Message?[] parts)
    {
        var list = new List<Message>();
        foreach (var part in parts)
        {
            if (part != null && !part.IsEmpty)
                list.Add(part);
        }
        return list.Count == 0 ? Empty : new Message(list, separator);
    }

    /// <summary>
    /// Compose two messages. Parts are joined with a space by default.
    /// Use Message.Sep() between parts for custom separators.
    /// </summary>
    public static Message operator +(Message left, Message right)
    {
        var parts = new List<Message>();
        Flatten(left, parts);
        Flatten(right, parts);
        return new Message(parts, " ");
    }

    /// <summary>Returns true if this message would resolve to null or empty.</summary>
    public bool IsEmpty
    {
        get
        {
            if (_parts != null)
                return _parts.Count == 0;
            if (_rawText != null)
                return string.IsNullOrEmpty(_rawText);
            // Localized message — can't know without resolving
            return false;
        }
    }

    /// <summary>
    /// Resolve the message through the full pipeline:
    /// localization lookup → variable substitution → BBCode stripping.
    /// </summary>
    public string Resolve()
    {
        if (_parts != null)
            return ResolveComposite();

        string text;

        if (_rawText != null)
        {
            text = _rawText;
        }
        else
        {
            text = LocalizationResolver?.Invoke(_table!, _key!) ?? $"MISSING({_table}.{_key})";
        }

        if (_vars != null && _vars.Count > 0)
            text = SubstituteVars(text, _vars);

        text = StripBbcode(text);

        return text;
    }

    public override string ToString() => Resolve();

    private string ResolveComposite()
    {
        var sb = new StringBuilder();
        foreach (var part in _parts!)
        {
            var resolved = part.Resolve();
            if (string.IsNullOrEmpty(resolved))
                continue;

            if (sb.Length > 0)
                sb.Append(_separator);
            sb.Append(resolved);
        }
        return sb.ToString();
    }

    private static void Flatten(Message msg, List<Message> into)
    {
        if (msg._parts != null)
        {
            foreach (var part in msg._parts)
                Flatten(part, into);
        }
        else
        {
            into.Add(msg);
        }
    }

    /// <summary>Strip BBCode tags, resolve icon image paths, and clean up res:// paths.</summary>
    public static string StripBbcode(string text)
    {
        text = ImgPattern.Replace(text, m => ResolveIconPath(m.Groups[1].Value));
        text = BbcodePattern.Replace(text, "");
        text = ResPathPattern.Replace(text, m => ResolveIconPath(m.Value));
        return text.Trim();
    }

    /// <summary>Register additional icon name mappings.</summary>
    public static void RegisterIcon(string suffix, string label)
    {
        IconNames[suffix] = label;
    }

    internal static string SubstituteVars(string text, Dictionary<string, string> vars)
    {
        return VariablePattern.Replace(text, match =>
        {
            var name = match.Groups[1].Value;
            return vars.TryGetValue(name, out var value) ? value : match.Value;
        });
    }

    internal static string ResolveIconPath(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        var name = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        var dot = name.LastIndexOf('.');
        if (dot > 0) name = name.Substring(0, dot);

        foreach (var (suffix, locKey) in IconNames)
        {
            if (name.EndsWith(suffix) || name == suffix)
                return LocalizationManager.GetOrDefault("ui", locKey, locKey);
        }

        name = name.Replace("_", " ").Trim();
        return name;
    }

    private static Dictionary<string, string> ObjectToDict(object obj)
    {
        var dict = new Dictionary<string, string>();
        foreach (var prop in obj.GetType().GetProperties())
        {
            var val = prop.GetValue(obj);
            dict[prop.Name] = val?.ToString() ?? "";
        }
        return dict;
    }
}
