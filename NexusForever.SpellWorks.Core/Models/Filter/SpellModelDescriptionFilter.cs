namespace NexusForever.SpellWorks.Core.Models.Filter
{
    public class SpellModelDescriptionFilter : ISpellModelFilter
    {
        public string Description { get; set; }

        public bool Filter(ISpellModel model)
        {
            if (Description != null && !model.Description.Contains(Description, StringComparison.InvariantCultureIgnoreCase))
                return false;

            return true;
        }
    }
}
