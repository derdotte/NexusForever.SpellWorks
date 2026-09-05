namespace NexusForever.SpellWorks.Core.Services
{
    /// <summary>
    /// Describes one game table exposed by <see cref="IGameTableService"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Values"/> projects an entry to its column values in <see cref="Columns"/> order using a
    /// compiled accessor - never reflection per cell.
    /// </remarks>
    public sealed record TableDescriptor(
        string Name,
        Type EntryType,
        int RowCount,
        IReadOnlyList<string> Columns,
        Func<IReadOnlyList<object>> Rows,
        Func<object, string[]> Values);

    public interface ITableCatalog
    {
        IReadOnlyList<TableDescriptor> Tables { get; }

        TableDescriptor Get(string name);

        /// <summary>
        /// Rebuild the catalog against the currently loaded game tables.
        /// </summary>
        void Rebuild();

        /// <summary>
        /// Drop every table. A failed load must not leave the previous archive's tables on show, which
        /// reads as if the client were still mounted.
        /// </summary>
        void Clear();
    }
}
