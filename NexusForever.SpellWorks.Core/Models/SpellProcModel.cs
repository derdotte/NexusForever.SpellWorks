using NexusForever.GameTable.Model;
using NexusForever.SpellWorks.Core.Static;

namespace NexusForever.SpellWorks.Core.Models
{
    public class SpellProcModel : ISpellProcModel
    {
        public ProcType ProcType => (ProcType)_entry.DataBits00;
        public uint SpellId => _entry.DataBits01;

        private Spell4EffectsEntry _entry;

        public void Initialise(Spell4EffectsEntry entry)
        {
            _entry = entry;
        }
    }
}
