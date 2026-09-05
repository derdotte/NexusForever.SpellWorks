namespace NexusForever.SpellWorks.Core.Models.Filter.Numeric
{
    /// <summary>
    /// A threshold on one numeric field of a spell.
    /// </summary>
    /// <remarks>
    /// The comparison is written once here and each named subclass supplies only the value it reads, because
    /// "is this at least N" is the same question whatever the column - and the client data mixes
    /// <c>uint</c> and <c>float</c> columns freely, so the comparison is done in <c>double</c> throughout.
    ///
    /// Every column these read lives on the <c>Spell4</c> row itself or on a list the model always has, so
    /// none of them can go missing the way a <c>Spell4Base</c> join can - which is why there is no
    /// absent case to handle here.
    /// </remarks>
    public abstract class SpellModelRangeFilter : ISpellModelFilter
    {
        public double Value { get; set; }

        /// <summary>Whether <see cref="Value"/> is a ceiling rather than a floor.</summary>
        public bool AtMost { get; set; }

        protected abstract double Read(ISpellModel model);

        public bool Filter(ISpellModel model)
        {
            double actual = Read(model);

            return AtMost ? actual <= Value : actual >= Value;
        }
    }
}
