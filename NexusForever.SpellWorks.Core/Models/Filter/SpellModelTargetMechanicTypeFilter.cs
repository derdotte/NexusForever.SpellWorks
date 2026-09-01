using NexusForever.SpellWorks.Core.Static;

namespace NexusForever.SpellWorks.Core.Models.Filter
{
    public class SpellModelTargetMechanicTypeFilter : ISpellModelFilter
    {
        public SpellTargetMechanicType TargetMechanicType { get; set; }

        public bool Filter(ISpellModel model)
        {
            return (SpellTargetMechanicType?)model.SpellBaseModel?.TargetMechanics.TargetType == TargetMechanicType;
        }
    }
}
