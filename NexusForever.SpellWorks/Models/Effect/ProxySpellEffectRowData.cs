using NexusForever.Game.Static.Spell;

namespace NexusForever.SpellWorks.Models.Effect
{
    [SpellEffect(SpellEffectType.Proxy)]
    public class ProxySpellEffectRowData : DefaultSpellEffectRowData
    {
        public override bool Data00IsHyperlink => true;
    }
}
