namespace NexusForever.SpellWorks.Core.Models.Filter.Effect
{
    /// <summary>A threshold on how much threat an effect generates, relative to its damage or healing.</summary>
    public class SpellEffectThreatFilter : IModelFilter<ISpellEffectModel>
    {
        public double Value { get; set; }

        /// <summary>Whether <see cref="Value"/> is a ceiling rather than a floor.</summary>
        public bool AtMost { get; set; }

        public bool Filter(ISpellEffectModel model)
        {
            if (model.Entry == null)
                return false;

            double actual = model.Entry.ThreatMultiplier;
            return AtMost ? actual <= Value : actual >= Value;
        }
    }
}
