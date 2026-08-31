using NexusForever.Game.Static.Spell;

namespace NexusForever.SpellWorks.Models.Effect
{
    [SpellEffect(SpellEffectType.Heal)]
    public class HealSpellEffectRowData : DefaultSpellEffectRowData
    {
        public override string Data00 => BitConverter.UInt32BitsToSingle(Entry.DataBits00).ToString();
    }
}
