using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace SayTheSpire2.Views;

/// <summary>
/// View over a lobby player struct. The July-31 beta renamed the game's
/// LobbyPlayer struct to StartRunLobbyPlayer (and added LoadRunLobbyPlayer);
/// stable keeps the old name. The field names are identical on both
/// branches, so every read goes through reflection on the runtime type and
/// the same build runs everywhere. Values are snapshotted at construction —
/// the source is a boxed struct, so there is nothing live to read from.
///
/// The `!` field lookups are intentional: a future field rename should crash
/// loudly rather than silently stop announcing lobby state.
/// </summary>
public sealed class LobbyPlayerView
{
    private readonly struct FieldSet
    {
        public FieldSet(Type type)
        {
            Id = AccessTools.Field(type, "id")!;
            Character = AccessTools.Field(type, "character")!;
            IsReady = AccessTools.Field(type, "isReady")!;
        }

        public System.Reflection.FieldInfo Id { get; }
        public System.Reflection.FieldInfo Character { get; }
        public System.Reflection.FieldInfo IsReady { get; }
    }

    private static readonly Dictionary<Type, FieldSet> FieldCache = new();

    public ulong Id { get; }
    public CharacterModel? Character { get; }
    public bool IsReady { get; }

    private LobbyPlayerView(ulong id, CharacterModel? character, bool isReady)
    {
        Id = id;
        Character = character;
        IsReady = isReady;
    }

    /// <summary>Wraps a boxed lobby player struct of either branch's type.</summary>
    public static LobbyPlayerView? FromBoxed(object? boxed)
    {
        if (boxed == null) return null;

        var type = boxed.GetType();
        if (!FieldCache.TryGetValue(type, out var fields))
        {
            fields = new FieldSet(type);
            FieldCache[type] = fields;
        }

        return new LobbyPlayerView(
            fields.Id.GetValue(boxed) is ulong id ? id : 0,
            fields.Character.GetValue(boxed) as CharacterModel,
            fields.IsReady.GetValue(boxed) is true);
    }

    /// <summary>
    /// The player id from a Harmony-boxed listener argument that is a bare
    /// ulong on one branch and a lobby player struct on the other (the
    /// November beta changed the load screens' PlayerConnected from
    /// <c>ulong playerId</c> to <c>LoadRunLobbyPlayer player</c>, which has
    /// an id but no character, so FromBoxed doesn't fit it).
    /// </summary>
    public static ulong IdOf(object boxed)
    {
        if (boxed is ulong id) return id;
        return AccessTools.Field(boxed.GetType(), "id")!.GetValue(boxed) is ulong fieldId ? fieldId : 0;
    }

    /// <summary>
    /// The lobby's LocalPlayer, read via reflection because the property's
    /// return type is the branch-divergent struct.
    /// </summary>
    public static LobbyPlayerView? LocalPlayerOf(object? lobby)
    {
        if (lobby == null) return null;
        var property = AccessTools.Property(lobby.GetType(), "LocalPlayer");
        return FromBoxed(property?.GetValue(lobby));
    }

    /// <summary>
    /// The lobby's Players list, read via reflection because the list's
    /// element type is the branch-divergent struct.
    /// </summary>
    public static IEnumerable<LobbyPlayerView> PlayersOf(object? lobby)
    {
        if (lobby == null) yield break;
        if (AccessTools.Property(lobby.GetType(), "Players")?.GetValue(lobby) is not IEnumerable players)
            yield break;
        foreach (var boxed in players)
        {
            if (FromBoxed(boxed) is { } view)
                yield return view;
        }
    }
}
