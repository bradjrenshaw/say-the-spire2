using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SayTheSpire2.Localization;

/// <summary>
/// A speech message that carries its text (raw or localized) and resolves it
/// through a consistent pipeline: localization lookup → variable substitution → BBCode stripping.
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

    private static readonly Dictionary<string, string> IconNames = new()
    {
        { "energy_icon", "Energy" },
        { "star_icon", "Star" },
        { "gold_icon", "Gold" },
        { "card_icon", "Card" },
        { "chest_icon", "Chest" },
    };

    private readonly string? _rawText;
    private readonly string? _table;
    private readonly string? _key;
    private readonly Dictionary<string, string>? _vars;

    private Message(string? rawText, string? table, string? key, Dictionary<string, string>? vars)
    {
        _rawText = rawText;
        _table = table;
        _key = key;
        _vars = vars;
    }

    /// <summary>Create a message from raw text.</summary>
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

    /// <summary>
    /// Resolve the message through the full pipeline:
    /// localization lookup → variable substitution → BBCode stripping.
    /// </summary>
    public string Resolve()
    {
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

        foreach (var (suffix, label) in IconNames)
        {
            if (name.EndsWith(suffix) || name == suffix)
                return label;
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
