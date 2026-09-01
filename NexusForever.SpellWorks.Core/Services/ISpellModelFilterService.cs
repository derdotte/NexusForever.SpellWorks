using NexusForever.SpellWorks.Core.Models;

namespace NexusForever.SpellWorks.Core.Services
{
    public interface ISpellModelFilterService
    {
        IEnumerable<ISpellModel> Filter(IEnumerable<ISpellModelFilter> filters, IEnumerable<ISpellModel> models);
    }
}