using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using SayTheSpire2.Localization;
namespace SayTheSpire2.Buffers;

public class UpgradeBuffer : Buffer
{
    private CardModel? _model;
    private CardModel? _previewModel;
    private bool _forceUnavailable;

    public UpgradeBuffer() : base("upgrade") { }

    private static string NoUpgradeText()
    {
        return LocalizationManager.GetOrDefault("ui", "CARD.UPGRADE_UNAVAILABLE", "No upgrade available");
    }

    public void Bind(CardModel model)
    {
        _model = model;
        _previewModel = null;
        _forceUnavailable = false;
    }

    public void Bind(CardModel model, CardModel? previewModel)
    {
        _model = model;
        _previewModel = previewModel;
        _forceUnavailable = false;
    }

    public void BindUnavailable()
    {
        _model = null;
        _previewModel = null;
        _forceUnavailable = true;
    }

    protected override void ClearBinding()
    {
        _model = null;
        _previewModel = null;
        _forceUnavailable = false;
        Clear();
    }

    public override void Update()
    {
        if (_forceUnavailable)
        {
            Repopulate(() => Add(NoUpgradeText()));
            return;
        }

        if (_model == null && _previewModel == null) return;
        Repopulate(Populate);
    }

    private void Populate()
    {
        var clone = ResolveUpgradeClone();
        if (clone == null)
        {
            Add(NoUpgradeText());
            return;
        }

        // The upgrade buffer shows the diff-style preview text ("Damage: 6 →
        // 9") rather than the plain upgraded-card description, so callers
        // can compare against the un-upgraded version they were just
        // looking at. Fall back to the regular description if the model
        // doesn't surface a preview.
        string? diff = null;
        try { diff = clone.GetDescriptionForUpgradePreview(); }
        catch (Exception e) { Log.Info($"[AccessibilityMod] Upgrade preview description access failed: {e.Message}"); }

        CardBuffer.Populate(this, clone, descriptionOverride: diff);
    }

    /// <summary>
    /// Returns the cloned-and-upgraded CardModel that the buffer should
    /// render, or null when no upgrade is available (caller writes the "No
    /// upgrade available" line). Routing every path through this means the
    /// buffer always renders with the same structure as the regular card
    /// buffer — same announcement order, same per-buffer settings cascade.
    /// </summary>
    private CardModel? ResolveUpgradeClone()
    {
        if (_previewModel != null)
            return _previewModel;

        var model = _model;
        if (model == null || !model.IsUpgradable)
            return null;

        // Beta 2026-04-23: CardScope can be a NullRunState sentinel instead
        // of null. Calling CloneCard on it throws, so treat both as "no
        // scope" and fall back to MutableClone.
        var cardScope = model.CardScope;
        try
        {
            CardModel clone = cardScope == null || cardScope is NullRunState
                ? (CardModel)model.MutableClone()
                : cardScope.CloneCard(model);
            clone.UpgradeInternal();
            return clone;
        }
        catch (Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade preview clone failed: {e.Message}");
            return null;
        }
    }
}
