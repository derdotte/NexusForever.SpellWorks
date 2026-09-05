using NexusForever.Game.Static.Spell;
using NexusForever.SpellWorks.Services.Filtering;

namespace NexusForever.SpellWorks.Services
{
    public enum PaneMode
    {
        Rows,
        Filter
    }

    public enum DetailSubTab
    {
        Spell,
        Effects,
        Procs
    }

    /// <summary>
    /// Per-pane state. Keyed by pane scope, so a view open twice - or popped out - carries two of these.
    /// </summary>
    public sealed class PaneState
    {
        public PaneMode Mode { get; set; } = PaneMode.Rows;

        /// <summary>
        /// This pane's boolean filter.
        /// </summary>
        /// <remarks>
        /// Get-only: both views capture it and the form's closures hold references to individual conditions,
        /// so swapping the object out would strand live edits against an orphan. Rehydration from a saved
        /// workspace goes through <see cref="FilterQuery.CopyFrom"/> instead.
        /// </remarks>
        public FilterQuery Filters { get; } = new();

        /// <summary>Set while this pane is locked to a spell; other panes keep navigating.</summary>
        public uint? LockedSpellId { get; set; }

        public DetailSubTab SubTab { get; set; } = DetailSubTab.Effects;

        /// <summary>Set while this pane is locked to an effect type, the analogue of <see cref="LockedSpellId"/>.</summary>
        public SpellEffectType? LockedEffectType { get; set; }
    }
}
