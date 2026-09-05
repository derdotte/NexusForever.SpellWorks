using System.Collections.Concurrent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.SpellWorks.Core.Models;
using NexusForever.SpellWorks.Core.Models.Filter;
using NexusForever.SpellWorks.Core.Models.Filter.Effect;
using NexusForever.SpellWorks.Core.Models.Filter.EffectType;
using NexusForever.SpellWorks.Core.Models.Filter.Numeric;
using NexusForever.SpellWorks.Core.Models.Filter.Proc;
using NexusForever.SpellWorks.Core.Models.Filter.Table;
using NexusForever.SpellWorks.Core.Services;
using NexusForever.SpellWorks.Core.Static;

namespace NexusForever.SpellWorks.Services.Filtering
{
    /// <summary>
    /// The schema for each pane kind: what it can be filtered on, and how each constraint becomes a filter.
    /// </summary>
    /// <remarks>
    /// Resolved from a <see cref="PaneDescriptor"/> rather than a bare <see cref="PaneKind"/> because a
    /// descriptor carries its table name, which is what a generic table's fields are built from. The fixed
    /// kinds are cached; a game table's schema is rebuilt per descriptor, since its columns are a runtime fact
    /// and the column a mask reads has to be resolved to an index before the row loop starts.
    ///
    /// The cache is concurrent because the registry is a singleton reached from two threads at once: a grid
    /// compiles its filter on the thread pool while the component that owns it renders its placeholder from
    /// the same schema. A plain dictionary corrupts itself when those land together.
    /// </remarks>
    public sealed class FilterSchemaRegistry
    {
        private readonly ConcurrentDictionary<PaneKind, FilterSchema> _cache = new();

        #region Dependency Injection

        private readonly ISpellModelService _spellModelService;
        private readonly ITableCatalog _tableCatalog;

        public FilterSchemaRegistry(
            ISpellModelService spellModelService,
            ITableCatalog tableCatalog)
        {
            _spellModelService = spellModelService;
            _tableCatalog      = tableCatalog;
        }

        #endregion

        public FilterSchema For(PaneDescriptor descriptor)
        {
            if (descriptor == null)
                return null;

            if (descriptor.Kind == PaneKind.GameTable)
                return BuildGameTable(descriptor);

            if (_cache.TryGetValue(descriptor.Kind, out FilterSchema cached))
                return cached;

            FilterSchema schema = descriptor.Kind switch
            {
                // The Effect Type spells pane lists Spell4 rows, so it carries the Spell4 schema verbatim -
                // narrowing a reverse lookup is the same job as narrowing the forward one.
                PaneKind.Spell4           => BuildSpell(),
                PaneKind.EffectTypeSpells => BuildSpell(),
                PaneKind.Effects          => BuildEffects(),
                PaneKind.Procs            => BuildProcs(),
                PaneKind.EffectTypes      => BuildEffectTypes(),
                PaneKind.Tables           => BuildTables(),
                _                         => null
            };

            if (schema != null)
                _cache[descriptor.Kind] = schema;

            return schema;
        }

        public FilterSchema<T> For<T>(PaneDescriptor descriptor) => For(descriptor) as FilterSchema<T>;

        /// <summary>Drop the cached schemas, so a reload's new tables are picked up.</summary>
        public void Invalidate() => _cache.Clear();

        // ------------------------------------------------------------------ spells

        private static FilterSchema<ISpellModel> BuildSpell()
        {
            const string general    = "Base · General";
            const string mechanic   = "Target Mechanic";
            const string effects    = "Effects";
            const string housekeeping = "Housekeeping";
            const string timing       = "Timing";
            const string reach        = "Reach";
            const string counts       = "Counts";

            return new FilterSchema<ISpellModel>(
            [
                Text<ISpellModel>(FilterFields.Id, "Id", general, "7157", FilterOperator.StartsWith,
                    c => new SpellModelIdFilter { IdPrefix = FilterValue.Trimmed(c.Value) }),

                // The localised name and the tooltip are dictionary lookups, so scanning them is cheap
                Text<ISpellModel>(FilterFields.Name, "Name", general, "Arcane Missile", FilterOperator.Contains,
                    c => new SpellModelTextFilter { Text = SpellText.Name, Query = FilterValue.Trimmed(c.Value) }),

                Text<ISpellModel>(FilterFields.Tooltip, "Tooltip", general, "damage over time",
                    FilterOperator.Contains,
                    c => new SpellModelTextFilter { Text = SpellText.Tooltip, Query = FilterValue.Trimmed(c.Value) }),

                Choice<ISpellModel, CastMethod>(FilterFields.CastMethod, "Cast Method", general,
                    v => new SpellModelCastMethodFilter { CastMethod = v }),

                Choice<ISpellModel, DamageType>(FilterFields.School, "School", general,
                    v => new SpellModelSchoolFilter { School = v }),

                Choice<ISpellModel, Class>(FilterFields.Class, "Class", general,
                    v => new SpellModelClassFilter { Class = v }),

                Choice<ISpellModel, SpellTargetMechanicType>(FilterFields.TargetMechanic, "Type", mechanic,
                    v => new SpellModelTargetMechanicTypeFilter { TargetMechanicType = v }),

                Flags<ISpellModel, SpellTargetMechanicFlags>(FilterFields.TargetMechanicFlags, "Flags", mechanic,
                    (v, mode) => new SpellModelTargetMechanicFlagsFilter { Flags = v, Mode = mode }),

                Choice<ISpellModel, SpellEffectType>(FilterFields.EffectType, "Effect Type", effects,
                    v => new SpellModelEffectTypeFilter { SpellEffectType = v }),

                Flags<ISpellModel, SpellEffectTargetFlags>(FilterFields.EffectTargetFlags, "Target Flags", effects,
                    (v, mode) => new SpellModelEffectTargetFlagsFilter { Flags = v, Mode = mode }),

                // Phrased positively; the form seeds this one negated, which is the old "hide deprecated".
                Toggle<ISpellModel>(FilterFields.Deprecated, "Deprecated", housekeeping,
                    _ => new SpellModelDeprecatedFilter()),

                Toggle<ISpellModel>(FilterFields.HasProcs, "Has procs", housekeeping,
                    _ => new SpellModelHasProcsFilter()),
                
                Range<ISpellModel, SpellModelCastTimeFilter>(FilterFields.CastTime, "Cast time", timing, "ms"),
                Range<ISpellModel, SpellModelDurationFilter>(FilterFields.Duration, "Duration", timing, "ms"),
                Range<ISpellModel, SpellModelCooldownFilter>(FilterFields.Cooldown, "Cooldown", timing, "ms"),
                Range<ISpellModel, SpellModelChannelTimeFilter>(FilterFields.ChannelTime, "Channel", timing, "ms"),
                Range<ISpellModel, SpellModelChannelPulseFilter>(FilterFields.ChannelPulse, "Channel pulse", timing, "ms"),

                Range<ISpellModel, SpellModelTierFilter>(FilterFields.Tier, "Tier", general, "1"),
                Range<ISpellModel, SpellModelAbilityChargesFilter>(FilterFields.AbilityCharges, "Charges", general, "1"),

                Range<ISpellModel, SpellModelTargetMinRangeFilter>(FilterFields.TargetMinRange, "Min range", reach, "0"),
                Range<ISpellModel, SpellModelTargetMaxRangeFilter>(FilterFields.TargetMaxRange, "Max range", reach, "30"),
                Range<ISpellModel, SpellModelTargetVerticalRangeFilter>(FilterFields.TargetVerticalRange, "Vertical", reach, "5"),
                Range<ISpellModel, SpellModelMissileSpeedFilter>(FilterFields.MissileSpeed, "Missile speed", reach, "20"),

                Range<ISpellModel, SpellModelEffectCountFilter>(FilterFields.EffectCount, "Effects", counts, "1"),
                Range<ISpellModel, SpellModelProcCountFilter>(FilterFields.ProcCount, "Procs", counts, "1"),
                Range<ISpellModel, SpellModelProcReferenceCountFilter>(FilterFields.ProcReferenceCount, "Referenced by", counts, "1")
            ],
            (term, exact) => new SpellModelTextSearchFilter { Query = term, Exact = exact },
            "Search description or name…",
            (term, exact) => new SpellModelIdSearchFilter { Query = term, Exact = exact },
            "Spell id…");
        }

        // ------------------------------------------------------------------ effects

        private static FilterSchema<ISpellEffectModel> BuildEffects()
        {
            const string effect     = "Effect";
            const string timing     = "Timing";
            const string internals  = "Row internals";
            const string parameters = "Parameters";

            return new FilterSchema<ISpellEffectModel>(
            [
                Choice<ISpellEffectModel, SpellEffectType>(FilterFields.EffectType, "Type", effect,
                    v => new SpellEffectTypeFilter { Type = v }),

                Mask<ISpellEffectModel>(FilterFields.EffectFlags, "Flags", effect,
                    (v, mode) => new SpellEffectFlagsFilter { Flags = v, Mode = mode }),

                Number<ISpellEffectModel>(FilterFields.EffectTick, "Tick", timing, "ms",
                    (v, atMost) => new SpellEffectTimingFilter
                    {
                        Timing = SpellEffectTiming.Tick, Value = v, AtMost = atMost
                    }),

                Number<ISpellEffectModel>(FilterFields.EffectDuration, "Duration", timing, "ms",
                    (v, atMost) => new SpellEffectTimingFilter
                    {
                        Timing = SpellEffectTiming.Duration, Value = v, AtMost = atMost
                    }),

                Number<ISpellEffectModel>(FilterFields.EffectDelay, "Delay", timing, "ms",
                    (v, atMost) => new SpellEffectTimingFilter
                    {
                        Timing = SpellEffectTiming.Delay, Value = v, AtMost = atMost
                    }),

                Threshold<ISpellEffectModel>(FilterFields.EffectThreat, "Threat", effect, "1.0",
                    (v, atMost) => new SpellEffectThreatFilter { Value = v, AtMost = atMost }),

                Choice<ISpellEffectModel, DamageType>(FilterFields.EffectDamage, "Damage", effect,
                    v => new SpellEffectDamageTypeFilter { DamageType = v }),

                Mask<ISpellEffectModel>(FilterFields.EffectPhase, "Phase flags", internals,
                    (v, mode) => new SpellEffectPhaseFlagsFilter { Flags = v, Mode = mode }),

                Number<ISpellEffectModel>(FilterFields.EffectOrder, "Order index", internals, "0",
                    (v, atMost) => new SpellEffectOrderIndexFilter { Value = v, AtMost = atMost }),

                Text<ISpellEffectModel>(FilterFields.EffectGroup, "Group list", internals, "0",
                    FilterOperator.Equals,
                    c => FilterValue.TryUInt(c.Value, out uint v)
                        ? new SpellEffectGroupListFilter { GroupListId = v }
                        : null),

                Choice<ISpellEffectModel, SpellEffectParameterType>(FilterFields.EffectParamType,
                    "Parameter", parameters,
                    v => new SpellEffectParameterFilter { ParameterType = v }),

                Text<ISpellEffectModel>(FilterFields.EffectEmmComparison, "EMM comparison", parameters, "0",
                    FilterOperator.Equals,
                    c => FilterValue.TryUInt(c.Value, out uint v)
                        ? new SpellEffectEmmFilter { Part = EmmPart.Comparison, Value = v }
                        : null),

                Text<ISpellEffectModel>(FilterFields.EffectEmmValue, "EMM value", parameters, "0",
                    FilterOperator.Equals,
                    c => FilterValue.TryUInt(c.Value, out uint v)
                        ? new SpellEffectEmmFilter { Part = EmmPart.Value, Value = v }
                        : null),

                Text<ISpellEffectModel>(FilterFields.EffectPrerequisite, "Prerequisite", parameters, "0",
                    FilterOperator.Equals,
                    c => FilterValue.TryUInt(c.Value, out uint v)
                        ? new SpellEffectPrerequisiteFilter
                        {
                            Slot = EffectPrerequisite.CasterApply, PrerequisiteId = v
                        }
                        : null),

                .. DataBitFields()
            ],
            (term, exact) => new SpellEffectNameSearchFilter { Query = term, Exact = exact },
            "Search effect type…",
            (term, exact) => new SpellEffectIdSearchFilter { Query = term, Exact = exact },
            "Type id…");
        }

        /// <summary>
        /// One field per <c>DataBits</c> column. They carry whatever the effect type needs - a spell id, a
        /// percentage, a packed bitfield - so each offers an exact value and both mask readings rather than
        /// the schema guessing which the column is.
        /// </summary>
        private static IEnumerable<FilterFieldSchema<ISpellEffectModel>> DataBitFields()
        {
            const string data = "Data bits";

            for (int i = 0; i < SpellEffectDataBitsFilter.Columns; i++)
            {
                int index = i;

                yield return new FilterFieldSchema<ISpellEffectModel>
                {
                    Key              = FilterFields.EffectData(index),
                    Label            = $"Data{index:00}",
                    GroupTitle       = data,
                    Control          = FilterControlKind.Mask,
                    Placeholder      = "value or 0x mask",
                    AllowedOperators = [FilterOperator.Equals, FilterOperator.MaskAll, FilterOperator.MaskAny],
                    Factory          = c => FilterValue.TryUInt(c.Value, out uint value)
                        ? new SpellEffectDataBitsFilter { Index = index, Value = value, Match = Match(c.Operator) }
                        : null
                };
            }
        }

        private static DataBitsMatch Match(FilterOperator op) => op switch
        {
            FilterOperator.MaskAll => DataBitsMatch.MaskAll,
            FilterOperator.MaskAny => DataBitsMatch.MaskAny,
            _                      => DataBitsMatch.Equals
        };

        // ------------------------------------------------------------------ procs

        private FilterSchema<ISpellProcModel> BuildProcs()
        {
            const string proc = "Proc";

            return new FilterSchema<ISpellProcModel>(
            [
                // Numeric, not a dropdown: Core.Static.ProcType is an empty enum, so there is nothing to
                // offer as options and nothing to parse a name against.
                Text<ISpellProcModel>(FilterFields.ProcType, "Proc Type", proc, "16", FilterOperator.Equals,
                    c => FilterValue.TryUInt(c.Value, out uint v) ? new SpellProcTypeFilter { ProcType = v } : null),

                Text<ISpellProcModel>(FilterFields.ProcSpellId, "Spell Id", proc, "7161", FilterOperator.StartsWith,
                    c => new SpellProcSpellIdFilter { IdPrefix = FilterValue.Trimmed(c.Value) }),

                Toggle<ISpellProcModel>(FilterFields.ProcReferenced, "Referenced", proc,
                    _ => new SpellProcReferencedFilter
                    {
                        ReferencedSpellIds = _spellModelService?.SpellProcReferences.Keys
                    })
            ],
            (term, exact) => new SpellProcTextSearchFilter
            {
                Query = term, Exact = exact, Description = DescriptionOf
            },
            "Search description…",
            (term, exact) => new SpellProcIdSearchFilter { Query = term, Exact = exact },
            "Spell id…");
        }

        private string DescriptionOf(uint spellId)
        {
            return _spellModelService != null
                && _spellModelService.SpellModels.TryGetValue(spellId, out ISpellModel model)
                    ? model.Description
                    : null;
        }

        // ------------------------------------------------------------------ effect types

        private static FilterSchema<EffectTypeUsage> BuildEffectTypes()
        {
            const string type = "Effect type";

            return new FilterSchema<EffectTypeUsage>(
            [
                Text<EffectTypeUsage>(FilterFields.TypeId, "Type Id starts", type, "17", FilterOperator.StartsWith,
                    c => new EffectTypeIdFilter { IdPrefix = FilterValue.Trimmed(c.Value) }),

                Number<EffectTypeUsage>(FilterFields.TypeSpells, "Spells", type, "10",
                    (v, atMost) => new EffectTypeSpellCountFilter { Value = v, AtMost = atMost })
            ],
            (term, exact) => new EffectTypeNameSearchFilter { Query = term, Exact = exact },
            "Search effect type…",
            (term, exact) => new EffectTypeIdSearchFilter { Query = term, Exact = exact },
            "Type id…");
        }

        // ------------------------------------------------------------------ table list

        private static FilterSchema<TableDescriptor> BuildTables()
        {
            const string archive = "Archive";

            return new FilterSchema<TableDescriptor>(
            [
                Text<TableDescriptor>(FilterFields.TableName, "Name starts", archive, "Spell4",
                    FilterOperator.StartsWith,
                    c => new TableNameFilter { Prefix = FilterValue.Trimmed(c.Value) }),

                Toggle<TableDescriptor>(FilterFields.TableLoaded, "Loaded", archive, _ => new TableLoadedFilter())
            ],
            // The only grid with no id box: a table is named, not numbered.
            (term, exact) => new TableSearchFilter { Query = term, Exact = exact },
            "Search table name…");
        }

        // ------------------------------------------------------------------ generic game table

        private FilterSchema<string[]> BuildGameTable(PaneDescriptor descriptor)
        {
            string row = $"{descriptor.TableName} · row";
            const string columns = "Columns";

            TableDescriptor table = _tableCatalog?.Get(descriptor.TableName);
            int flagsColumn = ColumnIndex(table, "Flags");

            return new FilterSchema<string[]>(
            [
                Text<string[]>(FilterFields.RowId, "Id starts", row, "1", FilterOperator.StartsWith,
                    c => new GameTableRowIdFilter { IdPrefix = FilterValue.Trimmed(c.Value) }),

                Text<string[]>(FilterFields.RowContains, "Contains", row, "any column value",
                    FilterOperator.Contains,
                    c => new GameTableContainsFilter { Query = FilterValue.Trimmed(c.Value) }),

                Mask<string[]>(FilterFields.RowFlags, "Flags", columns,
                    (v, mode) => flagsColumn < 0
                        // This table has no Flags column, so a mask on it is not a constraint the table can
                        // answer - silently emptying the grid instead would read as a broken table.
                        ? MatchAllFilter<string[]>.Instance
                        : new GameTableCellFilter { ColumnIndex = flagsColumn, Mask = v, Mode = mode }),

                Toggle<string[]>(FilterFields.RowNonZero, "Non-zero", columns, _ => new GameTableNonZeroFilter()),

                .. ColumnFields(table)
            ],
            (term, exact) => new GameTableContainsFilter { Query = term, Exact = exact },
            "Search any column…",
            (term, exact) => new GameTableColumnFilter { ColumnIndex = 0, Query = term, Exact = exact },
            "Row id…");
        }

        /// <summary>
        /// One field per column of the table, so any of the tables becomes queryable by column rather than only
        /// by "some cell somewhere contains this".
        /// </summary>
        /// <remarks>
        /// The field list comes free from the descriptor and the values from its compiled accessor, so this
        /// costs nothing per table beyond resolving each column to its index once.
        /// </remarks>
        private static IEnumerable<FilterFieldSchema<string[]>> ColumnFields(TableDescriptor table)
        {
            if (table == null)
                yield break;

            const string card = "Columns · by name";

            for (int i = 0; i < table.Columns.Count; i++)
            {
                int index = i;
                string column = table.Columns[index];

                yield return new FilterFieldSchema<string[]>
                {
                    Key              = FilterFields.Column(column),
                    Label            = column,
                    GroupTitle       = card,
                    Control          = FilterControlKind.Text,
                    Placeholder      = "value",
                    AllowedOperators = [FilterOperator.Contains, FilterOperator.Equals],
                    Factory          = c => new GameTableColumnFilter
                    {
                        ColumnIndex = index,
                        Query       = FilterValue.Trimmed(c.Value),
                        Exact       = c.Operator == FilterOperator.Equals
                    }
                };
            }
        }

        private static int ColumnIndex(TableDescriptor table, string columnName)
        {
            if (table == null)
                return -1;

            for (int i = 0; i < table.Columns.Count; i++)
                if (table.Columns[i].Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;

            return -1;
        }

        // ------------------------------------------------------------------ field constructors

        private static FilterFieldSchema<T> Text<T>(
            string key, string label, string card, string placeholder, FilterOperator op,
            Func<FilterCondition, IModelFilter<T>> factory)
        {
            return new FilterFieldSchema<T>
            {
                Key              = key,
                Label            = label,
                GroupTitle       = card,
                Control          = FilterControlKind.Text,
                Placeholder      = placeholder,
                AllowedOperators = [op],
                Factory          = factory
            };
        }

        private static FilterFieldSchema<T> Choice<T, TEnum>(
            string key, string label, string card, Func<TEnum, IModelFilter<T>> factory)
            where TEnum : struct, Enum
        {
            return new FilterFieldSchema<T>
            {
                Key              = key,
                Label            = label,
                GroupTitle       = card,
                Control          = FilterControlKind.Choice,
                // Option lists come from the engine's enums, never from a hard-coded array.
                Options          = [FilterFieldSchema.Any, .. Enum.GetNames<TEnum>()],
                AllowedOperators = [FilterOperator.Equals],
                Factory          = c => FilterValue.TryEnum(c.Value, out TEnum value) ? factory(value) : null
            };
        }

        /// <summary>
        /// A bitmask whose bits have names, so the form can offer a picker. The stored value is still the
        /// assembled number, so a picked mask and a typed one persist and compile identically.
        /// </summary>
        private static FilterFieldSchema<T> Flags<T, TEnum>(
            string key, string label, string card, Func<uint, MaskMode, IModelFilter<T>> factory)
            where TEnum : struct, Enum
        {
            return new FilterFieldSchema<T>
            {
                Key              = key,
                Label            = label,
                GroupTitle       = card,
                Control          = FilterControlKind.Flags,
                Placeholder      = "bitmask",
                Bits             = EnumText.Bits<TEnum>(),
                AllowedOperators = [FilterOperator.MaskAll, FilterOperator.MaskAny],
                Factory          = c => FilterValue.TryUInt(c.Value, out uint value)
                    ? factory(value, FilterValue.MaskMode(c))
                    : null
            };
        }

        private static FilterFieldSchema<T> Mask<T>(
            string key, string label, string card, Func<uint, MaskMode, IModelFilter<T>> factory)
        {
            return new FilterFieldSchema<T>
            {
                Key              = key,
                Label            = label,
                GroupTitle       = card,
                Control          = FilterControlKind.Mask,
                Placeholder      = "bitmask",
                AllowedOperators = [FilterOperator.MaskAll, FilterOperator.MaskAny],
                Factory          = c => FilterValue.TryUInt(c.Value, out uint value)
                    ? factory(value, FilterValue.MaskMode(c))
                    : null
            };
        }

        private static FilterFieldSchema<T> Number<T>(
            string key, string label, string card, string placeholder,
            Func<uint, bool, IModelFilter<T>> factory)
        {
            return new FilterFieldSchema<T>
            {
                Key              = key,
                Label            = label,
                GroupTitle       = card,
                Control          = FilterControlKind.Text,
                Placeholder      = placeholder,
                AllowedOperators = [FilterOperator.AtLeast, FilterOperator.AtMost],
                Factory          = c => FilterValue.TryUInt(c.Value, out uint value)
                    ? factory(value, c.Operator == FilterOperator.AtMost)
                    : null
            };
        }

        /// <summary>
        /// A threshold on a decimal-capable value. Distinct from <see cref="Number{T}"/>, which is for the
        /// whole-number columns and keeps their <c>uint</c> parse.
        /// </summary>
        private static FilterFieldSchema<T> Threshold<T>(
            string key, string label, string card, string placeholder,
            Func<double, bool, IModelFilter<T>> factory)
        {
            return new FilterFieldSchema<T>
            {
                Key              = key,
                Label            = label,
                GroupTitle       = card,
                Control          = FilterControlKind.Text,
                Placeholder      = placeholder,
                AllowedOperators = [FilterOperator.AtLeast, FilterOperator.AtMost],
                Factory          = c => FilterValue.TryNumber(c.Value, out double value)
                    ? factory(value, c.Operator == FilterOperator.AtMost)
                    : null
            };
        }

        /// <summary>
        /// A threshold backed by one of the named <see cref="SpellModelRangeFilter"/> subclasses - the whole
        /// of a numeric field's wiring is its type, its key and its label.
        /// </summary>
        private static FilterFieldSchema<T> Range<T, TFilter>(
            string key, string label, string card, string placeholder)
            where TFilter : SpellModelRangeFilter, IModelFilter<T>, new()
        {
            return Threshold<T>(key, label, card, placeholder,
                (v, atMost) => new TFilter { Value = v, AtMost = atMost });
        }

        private static FilterFieldSchema<T> Toggle<T>(
            string key, string label, string card, Func<FilterCondition, IModelFilter<T>> factory)
        {
            return new FilterFieldSchema<T>
            {
                Key              = key,
                Label            = label,
                GroupTitle       = card,
                Control          = FilterControlKind.Toggle,
                AllowedOperators = [FilterOperator.IsSet],
                Factory          = factory
            };
        }
    }
}
