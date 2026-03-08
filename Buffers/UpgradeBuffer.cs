using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
namespace SayTheSpire2.Buffers;

public class UpgradeBuffer : Buffer
{
    private CardModel? _model;

    public UpgradeBuffer() : base("upgrade") { }

    public void Bind(CardModel model)
    {
        _model = model;
    }

    protected override void ClearBinding()
    {
        _model = null;
        Clear();
    }

    public override void Update()
    {
        if (_model == null) return;
        Repopulate(Populate);
    }

    private void Populate()
    {
        var model = _model;
        if (model == null) return;

        if (!model.IsUpgradable)
        {
            Add("No upgrade available");
            return;
        }

        if (model.CardScope == null) return;

        try
        {
            var clone = model.CardScope.CloneCard(model);
            clone.UpgradeInternal();

            Add(clone.Title);
            var typeRarity = clone.Type.ToString();
            if (clone.Rarity != CardRarity.Common)
                typeRarity += $", {clone.Rarity}";
            Add(typeRarity);

            if (clone.EnergyCost != null)
            {
                if (clone.EnergyCost.CostsX)
                    Add("X energy");
                else
                    Add($"{clone.EnergyCost.GetWithModifiers(CostModifiers.All)} energy");
            }

            if (clone.CurrentStarCost > 0)
                Add($"{clone.CurrentStarCost}");

            try
            {
                var desc = clone.GetDescriptionForUpgradePreview();
                if (!string.IsNullOrEmpty(desc))
                    Add(desc);
            }
            catch { }
        }
        catch (System.Exception e)
        {
            Log.Error($"[AccessibilityMod] Card upgrade preview failed: {e.Message}");
            Add("Upgrade preview unavailable");
        }
    }
}
