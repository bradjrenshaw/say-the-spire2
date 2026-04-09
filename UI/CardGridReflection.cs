using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace SayTheSpire2.UI;

/// <summary>
/// Cached reflection accessors for NCardGrid internals.
/// Used by CardPileGameScreen, CardGridSelectionGameScreen, and CardLibraryGameScreen.
/// </summary>
public static class CardGridReflection
{
    public static readonly FieldInfo? CardRowsField =
        AccessTools.Field(typeof(NCardGrid), "_cardRows");

    public static readonly PropertyInfo? ColumnsProperty =
        AccessTools.Property(typeof(NCardGrid), "Columns");

    public static List<List<Control>>? GetCardRows(NCardGrid grid)
    {
        return CardRowsField?.GetValue(grid) as List<List<Control>>;
    }

    public static int GetColumns(NCardGrid grid)
    {
        return ColumnsProperty?.GetValue(grid) as int? ?? 1;
    }
}
