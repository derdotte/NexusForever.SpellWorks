using NexusForever.SpellWorks.Core.Models;

namespace NexusForever.SpellWorks.Services.Filtering
{
    /// <summary>
    /// Builds the filter for one search term: the text typed, and whether the box is asking for a whole
    /// value rather than a substring.
    /// </summary>
    public delegate IModelFilter<T> SearchTermFactory<T>(string term, bool exact);

    /// <summary>
    /// Every field one pane can be filtered on, in form order.
    /// </summary>
    public abstract class FilterSchema
    {
        public IReadOnlyList<FilterFieldSchema> Fields { get; protected init; } = [];

        /// <summary>Whether this pane has anything numbered to search, and so draws the id box.</summary>
        public abstract bool HasIdSearch { get; }

        /// <summary>What the text box invites the user to type.</summary>
        public string TextPlaceholder { get; protected init; } = "Search…";

        /// <summary>What the id box invites the user to type. Null when the pane has no id box.</summary>
        public string IdPlaceholder { get; protected init; }

        private Dictionary<string, FilterFieldSchema> _byKey;

        /// <summary>The field <paramref name="key"/> names, or null if the schema no longer has one.</summary>
        public FilterFieldSchema Field(string key)
        {
            _byKey ??= Fields.ToDictionary(f => f.Key);

            return key != null && _byKey.TryGetValue(key, out FilterFieldSchema field) ? field : null;
        }

        /// <summary>Fields grouped into the titled cards the form draws, in declaration order.</summary>
        public IEnumerable<IGrouping<string, FilterFieldSchema>> Cards => Fields.GroupBy(f => f.GroupTitle);
    }

    /// <summary>A schema for a pane listing <typeparamref name="T"/>.</summary>
    public sealed class FilterSchema<T> : FilterSchema
    {
        public FilterSchema(
            IReadOnlyList<FilterFieldSchema<T>> fields,
            SearchTermFactory<T> textTerm,
            string textPlaceholder,
            SearchTermFactory<T> idTerm = null,
            string idPlaceholder = null)
        {
            base.Fields          = fields;
            TextTerm             = textTerm;
            IdTerm               = idTerm;
            base.TextPlaceholder = textPlaceholder;
            base.IdPlaceholder   = idPlaceholder;
        }

        /// <summary>
        /// What one word in the text box means here - the description or name on the spell panes, the effect
        /// type's name on the Effects pane, any cell on a generic table.
        /// </summary>
        public SearchTermFactory<T> TextTerm { get; }

        /// <summary>
        /// What one term in the id box means here, or null on a pane with nothing numbered to search - the
        /// table list, whose rows are named rather than numbered, is the only one. The form draws the second
        /// box only where this is set.
        /// </summary>
        public SearchTermFactory<T> IdTerm { get; }

        public override bool HasIdSearch => IdTerm != null;

        /// <summary>The typed field <paramref name="key"/> names, or null.</summary>
        public new FilterFieldSchema<T> Field(string key) => (FilterFieldSchema<T>)base.Field(key);
    }
}
