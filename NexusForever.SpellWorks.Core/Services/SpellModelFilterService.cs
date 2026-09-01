using NexusForever.SpellWorks.Core.Models;

namespace NexusForever.SpellWorks.Core.Services
{
    public class SpellModelFilterService : ISpellModelFilterService
    {
        public IEnumerable<ISpellModel> Filter(IEnumerable<ISpellModelFilter> filters, IEnumerable<ISpellModel> models)
        {
            foreach (ISpellModel model in models)
                if (filters.All(f => f.Filter(model)))
                    yield return model;
        }
    }
}
