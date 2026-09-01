using NexusForever.GameTable.Model;
using NexusForever.SpellWorks.Core.Static;

namespace NexusForever.SpellWorks.Core.Models
{
    public interface ISpellProcModel
    {
        ProcType ProcType { get; }
        uint SpellId { get; }

        void Initialise(Spell4EffectsEntry entry);
    }
}