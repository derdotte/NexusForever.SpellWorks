using NexusForever.GameTable.Model;
using NexusForever.SpellWorks.Static;

namespace NexusForever.SpellWorks.Models
{
    public interface ISpellProcModel
    {
        ProcType ProcType { get; }
        uint SpellId { get; }

        void Initialise(Spell4EffectsEntry entry);
    }
}