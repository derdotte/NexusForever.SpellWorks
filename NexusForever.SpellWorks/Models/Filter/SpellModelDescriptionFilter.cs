namespace NexusForever.SpellWorks.Models.Filter
{
    public class SpellModelDescriptionFilter : ISpellModelFilter
    {
        public string Description { get; set; }

        public bool UseFuzzy { get; set; } = false;
        

        // TODO: Expose in Settings
        public double FuzzyThreshold { get; set; } = 0.7;

        public bool Filter(ISpellModel model)
        {
            if (string.IsNullOrWhiteSpace(Description))
                return true;

            if (model?.Description == null)
                return false;

            if (!UseFuzzy)
            {
                return model.Description.Contains(Description, StringComparison.InvariantCultureIgnoreCase);
            }

            return FuzzyMatcher.IsFuzzyMatch(model.Description, Description, FuzzyThreshold);
        }
    }
}