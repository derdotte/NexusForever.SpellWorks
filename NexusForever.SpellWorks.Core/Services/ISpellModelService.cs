using NexusForever.SpellWorks.Core.Models;

namespace NexusForever.SpellWorks.Core.Services
{
    public interface ISpellModelService
    {
        Dictionary<uint, ISpellBaseModel> SpellBaseModels { get; }
        Dictionary<uint, ISpellModel> SpellModels { get; }
        Dictionary<uint, List<ISpellEffectModel>> SpellEffectModels { get; }
        Dictionary<uint, List<ISpellProcModel>> SpellProcModels { get; }
        Dictionary<uint, List<uint>> SpellProcReferences { get; }

        Task Initialise(IProgress<EngineProgress> progress);
    }
}