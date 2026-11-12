using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace SayTheSpire2.Views;

/// <summary>
/// View over a Blitz merchant offer (<c>BlitzMerchantEntry</c>): the free
/// "pick N of these" wares the Blitz shop shows instead of priced cards,
/// relics and potions. Each offer is a title plus a description; cursed
/// and rare offers say so in that text, so there is nothing else to read.
///
/// The type only exists on the November beta, so it is resolved by name and
/// every read goes through reflection — a direct reference would fail to
/// load on stable. <see cref="FromEntry"/> returns null on stable and for
/// ordinary shop entries.
/// </summary>
public sealed class BlitzMerchantEntryView
{
    private static readonly Type? EntryType =
        AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Merchant.BlitzMerchantEntry");

    // Resolved only where the type exists; a rename there should crash.
    private static readonly PropertyInfo? TitleProp =
        EntryType == null ? null : AccessTools.Property(EntryType, "Title")!;
    private static readonly PropertyInfo? DescriptionProp =
        EntryType == null ? null : AccessTools.Property(EntryType, "Description")!;
    private static readonly PropertyInfo? HoverTipsProp =
        EntryType == null ? null : AccessTools.Property(EntryType, "HoverTips")!;

    private readonly MerchantEntry _entry;

    private BlitzMerchantEntryView(MerchantEntry entry) { _entry = entry; }

    public static BlitzMerchantEntryView? FromEntry(MerchantEntry? entry) =>
        entry != null && EntryType != null && EntryType.IsInstanceOfType(entry)
            ? new BlitzMerchantEntryView(entry)
            : null;

    public string Title => Format(TitleProp);

    /// <summary>The offer's description, with the game's BBCode markup intact.</summary>
    public string Description => Format(DescriptionProp);

    /// <summary>
    /// The offer's hover tips as the game shows them. The first is always the
    /// offer's own title and description; any after it explain a keyword the
    /// offer references (e.g. the enchantment it applies).
    /// </summary>
    public IEnumerable<IHoverTip> HoverTips =>
        HoverTipsProp?.GetValue(_entry) as IEnumerable<IHoverTip> ?? Array.Empty<IHoverTip>();

    private string Format(PropertyInfo? property) =>
        property?.GetValue(_entry) is LocString text ? text.GetFormattedText() : "";
}
