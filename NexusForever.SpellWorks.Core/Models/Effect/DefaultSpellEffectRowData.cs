using NexusForever.GameTable.Model;

namespace NexusForever.SpellWorks.Core.Models.Effect
{
    public class DefaultSpellEffectRowData : ISpellEffectRowData
    {
        public Spell4EffectsEntry Entry { get; set; }

        public virtual string Data00 => Entry.DataBits00.ToString();
        public virtual bool Data00IsHyperlink => false;
        public virtual string Data01 => Entry.DataBits01.ToString();
        public virtual string Data02 => Entry.DataBits02.ToString();
        public virtual string Data03 => Entry.DataBits03.ToString();
        public virtual string Data04 => Entry.DataBits04.ToString();
        public virtual string Data05 => Entry.DataBits05.ToString();
        public virtual string Data06 => Entry.DataBits06.ToString();
        public virtual string Data07 => Entry.DataBits07.ToString();
        public virtual string Data08 => Entry.DataBits08.ToString();
        public virtual string Data09 => Entry.DataBits09.ToString();
    }
}
