using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using SayTheSpire2.Localization;
using SayTheSpire2.Multiplayer;
using SayTheSpire2.Views;
namespace SayTheSpire2.Buffers;

public class CreatureBuffer : Buffer
{
    private Creature? _creature;

    public CreatureBuffer() : base("creature") { }

    public void Bind(Creature creature)
    {
        _creature = creature;
    }

    protected override void ClearBinding()
    {
        _creature = null;
        Clear();
    }

    public override void Update()
    {
        if (_creature == null) return;
        Repopulate(Populate);
    }

    private void Populate()
    {
        var entity = _creature;
        if (entity == null) return;

        // Name
        Add(MultiplayerHelper.GetCreatureName(entity));

        // HP
        Add(Message.Localized("ui", "RESOURCE.HP", new { current = entity.CurrentHp, max = entity.MaxHp }).Resolve());

        // Block
        if (entity.Block > 0)
            Add(Message.Localized("ui", "RESOURCE.BLOCK", new { amount = entity.Block }).Resolve());

        // Intents for monsters
        if (entity.IsMonster && entity.Monster != null)
        {
            try
            {
                var intents = entity.Monster.NextMove.Intents;
                if (intents != null && intents.Count > 0)
                {
                    var allies = CreatureView.GetCombatStateAllies(entity);
                    foreach (var intent in intents)
                    {
                        var tip = intent.GetHoverTip(allies, entity);
                        var intentText = tip.Title ?? intent.IntentType.ToString();
                        if (!string.IsNullOrEmpty(tip.Description))
                            intentText += ": " + tip.Description;
                        Add(intentText);
                    }
                }
            }
            catch
            {
                // Intent access may fail outside combat
            }
        }

        // Powers (buffs/debuffs)
        if (entity.Powers.Count > 0)
        {
            foreach (var power in entity.Powers)
            {
                PlayerBuffer.AddPowerToBuffer(this, power);
            }
        }
    }
}
