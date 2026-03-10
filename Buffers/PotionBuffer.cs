using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using SayTheSpire2.UI.Elements;
namespace SayTheSpire2.Buffers;

public class PotionBuffer : Buffer
{
    private PotionModel? _model;

    public PotionBuffer() : base("potion") { }

    public void Bind(PotionModel model)
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
        Repopulate(() => Populate(this, _model));
    }

    /// <summary>
    /// Single source of truth for populating any buffer with potion data.
    /// </summary>
    public static void Populate(Buffer buffer, PotionModel model)
    {
        var title = model.Title.GetFormattedText();
        var rarity = model.Rarity;
        if (rarity != PotionRarity.None)
            buffer.Add($"{title}, {rarity}");
        else
            buffer.Add(title);

        var desc = model.DynamicDescription.GetFormattedText();
        if (!string.IsNullOrEmpty(desc))
            buffer.Add(ProxyElement.StripBbcode(desc));

        // Hover tips: skip first (it's the potion itself), rest are keywords
        try
        {
            bool first = true;
            foreach (var tip in model.HoverTips)
            {
                if (first) { first = false; continue; }
                if (tip is HoverTip hoverTip)
                {
                    var tipTitle = hoverTip.Title;
                    var tipDesc = hoverTip.Description;
                    if (!string.IsNullOrEmpty(tipTitle) && !string.IsNullOrEmpty(tipDesc))
                        buffer.Add($"{tipTitle}: {ProxyElement.StripBbcode(tipDesc)}");
                    else if (!string.IsNullOrEmpty(tipTitle))
                        buffer.Add(tipTitle);
                }
            }
        }
        catch { }
    }
}
