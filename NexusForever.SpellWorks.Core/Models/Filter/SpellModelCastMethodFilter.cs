using NexusForever.SpellWorks.Core.Static;

namespace NexusForever.SpellWorks.Core.Models.Filter
{
    public class SpellModelCastMethodFilter : ISpellModelFilter
    {
        public CastMethod CastMethod { get; set; }

        public bool Filter(ISpellModel model)
        {
            return (CastMethod)model.SpellBaseModel.Entry.CastMethod == CastMethod;
        }
    }
}
