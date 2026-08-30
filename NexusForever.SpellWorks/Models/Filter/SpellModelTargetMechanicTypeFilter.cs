using NexusForever.SpellWorks.Static;

namespace NexusForever.SpellWorks.Models.Filter
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
