using NexusForever.SpellWorks.Core.Models;
using NexusForever.SpellWorks.Core.Models.Filter;
using NexusForever.SpellWorks.Core.Services;

namespace NexusForever.SpellWorks.Services.Filtering
{
    /// <summary>Why a condition did not make it into the compiled filter.</summary>
    public enum FilterDiagnosticKind
    {
        /// <summary>The field key is not in this pane's schema - structurally meaningless, so dropped.</summary>
        UnknownField,

        /// <summary>The value does not parse. Dropped from the filter, but kept and marked in the form.</summary>
        UnparseableValue,

        /// <summary>A whole OR block compiled to nothing and was dropped rather than made true.</summary>
        EmptyGroup
    }

    public sealed record FilterDiagnostic(FilterDiagnosticKind Kind, string Field);

    /// <summary>
    /// Folds a <see cref="FilterQuery"/> into one Core filter:
    /// <c>AllOf(search, common, AnyOf(groups))</c>.
    /// </summary>
    public static class FilterQueryCompiler
    {
        public static IModelFilter<T> Compile<T>(FilterQuery query, FilterSchema<T> schema) =>
            Compile(query, schema, out _);

        public static IModelFilter<T> Compile<T>(
            FilterQuery query, FilterSchema<T> schema, out IReadOnlyList<FilterDiagnostic> diagnostics)
        {
            List<FilterDiagnostic> found = [];
            diagnostics = found;

            if (query == null || schema == null)
                return MatchAllFilter<T>.Instance;

            List<IModelFilter<T>> conjunction = [];

            // The two boxes are separate questions AND-ed together, so "7157" in the id box and "damage" in
            // the text box asks for both rather than for rows matching either.
            Add(conjunction, query.Search, schema.TextTerm, query.ExactSearch);
            Add(conjunction, query.IdSearch, schema.IdTerm, query.ExactSearch);

            // The common band is AND-ed once around the whole disjunction rather than copied into each block,
            // which is both cheaper and what "applies to all blocks" says on screen.
            conjunction.AddRange(CompileGroup(query.Common, schema, found));

            List<IModelFilter<T>> alternatives = [];
            foreach (FilterGroup group in query.Groups)
            {
                List<IModelFilter<T>> conditions = CompileGroup(group, schema, found);

                // A block that compiled to nothing is dropped, never treated as true: one unparseable term
                // inside an OR would otherwise silently widen the grid to every row.
                if (conditions.Count > 0)
                    alternatives.Add(new AllOfFilter<T>(conditions));
                else if (group.Conditions.Count > 0)
                    found.Add(new FilterDiagnostic(FilterDiagnosticKind.EmptyGroup, null));
            }

            if (alternatives.Count > 0)
                conjunction.Add(new AnyOfFilter<T>(alternatives));

            return conjunction.Count > 0 ? new AllOfFilter<T>(conjunction) : MatchAllFilter<T>.Instance;
        }

        private static void Add<T>(
            List<IModelFilter<T>> conjunction, string typed, SearchTermFactory<T> factory, bool exact)
        {
            if (factory == null || string.IsNullOrWhiteSpace(typed))
                return;

            IModelFilter<T> search = SearchExpression.Parse(typed).Compile(term => factory(term, exact));

            if (search is not MatchAllFilter<T>)
                conjunction.Add(search);
        }

        private static List<IModelFilter<T>> CompileGroup<T>(
            FilterGroup group, FilterSchema<T> schema, List<FilterDiagnostic> diagnostics)
        {
            List<IModelFilter<T>> compiled = [];

            foreach (FilterCondition condition in group.Conditions)
            {
                FilterFieldSchema<T> field = schema.Field(condition.Field);
                if (field == null)
                {
                    diagnostics.Add(new FilterDiagnostic(FilterDiagnosticKind.UnknownField, condition.Field));
                    continue;
                }

                // An untouched control asks for nothing. Not an error, and not a reason to drop the block.
                if (field.IsBlank(condition))
                    continue;

                IModelFilter<T> filter = field.Factory(condition);
                if (filter == null)
                {
                    diagnostics.Add(new FilterDiagnostic(FilterDiagnosticKind.UnparseableValue, condition.Field));
                    continue;
                }

                compiled.Add(condition.Negate ? new NotFilter<T>(filter) : filter);
            }

            return compiled;
        }
    }
}
