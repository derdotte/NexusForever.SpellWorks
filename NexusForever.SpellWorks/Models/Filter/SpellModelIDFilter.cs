using NexusForever.SpellWorks.GameTable.Static;

namespace NexusForever.SpellWorks.Models.Filter
{
    public class SpellModelIDFilter : ISpellModelFilter
    {
        public int Spell4Id { get; set; }

        public bool useIDSearch { get; set; } = false;
        public bool Filter(ISpellModel model)
        {
            // TODO: include Fuzzy matching?
            if (!useIDSearch)
                return false;
            return model.Id == Spell4Id;
        }
    }
}