using System.IO;
using NexusForever.GameTable;

namespace NexusForever.SpellWorks.Core.Services
{
    public class TextTableService : ITextTableService
    {
        private readonly Dictionary<string, TextTable> tables = new(StringComparer.OrdinalIgnoreCase);

        private string locale;


        private string tableName;

        /// <summary>
        /// File name of the localisation table currently in use.
        /// </summary>
        /// <remarks>
        /// Resolved on read. The name follows whichever table <see cref="Locale"/> lands on, and a caller
        /// may ask for it before anything has read a string - as the engine does when it describes a load.
        /// </remarks>
        public string TableName
        {
            get
            {
                Resolve();
                return tableName;
            }
        }

        /// <summary>
        /// Locale tag selecting which loaded text table <see cref="GetText"/> reads from.
        /// </summary>
        public string Locale
        {
            get => locale;
            set
            {
                locale = value;
                Resolve();
            }
        }

        /// <summary>
        /// Locale tags actually available in the mounted archives, in load order.
        /// </summary>
        public IReadOnlyList<string> AvailableLocales => tables.Keys.ToList();

        /// <summary>
        /// Number of localised strings in the current table.
        /// </summary>
        public int EntryCount => Resolve()?.Entries.Length ?? 0;

        /// <summary>
        /// Pick the table for <see cref="Locale"/>, falling back to the first archive mounted - English,
        /// as <c>ArchiveService</c> orders them - when the locale asked for is not among them.
        /// </summary>
        private TextTable Resolve()
        {
            if (tables.Count == 0)
            {
                tableName = null;
                return null;
            }

            if (locale != null && tables.TryGetValue(locale, out TextTable match))
            {
                tableName = locale + ".bin";
                return match;
            }

            KeyValuePair<string, TextTable> first = tables.First();
            tableName = first.Key + ".bin";
            return first.Value;
        }

        /// <summary>
        /// <c>en-US.bin</c> -> <c>enUS</c>, matching the locale tags the UI offers.
        /// </summary>
        private static string LocaleOf(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName ?? "").Replace("-", "").Replace("_", "");
        }

        #region Dependency Injection

        private readonly IArchiveService _archiveService;

        public TextTableService(
            IArchiveService archiveService)
        {
            _archiveService = archiveService;
        }

        #endregion

        private int count;

        public async Task Initialise(IProgress<EngineProgress> progress)
        {
            tables.Clear();
            tableName = null;
            count     = 0;

            // The files are gathered before anything is read, in archive order. The count of them is what
            // the load reports against - an archive may hold more than one locale, and counting archives
            // would then announce a maximum the load walks straight past.
            List<IArchiveFile> files = [];
            foreach (IArchiveReader archive in _archiveService.LocalisationArchives)
                files.AddRange(archive.Search("*.bin"));

            int total = files.Count;
            progress.Report(new EngineProgress("Loading Text Tables...", 0, 0, total));

            TextTable[] loaded = await Task.WhenAll(files.Select(file => LoadTextTable(progress, file, total)));

            // Recorded after the load rather than inside it. A dictionary enumerates in insertion order and
            // Resolve falls back to the first entry, so inserting as each task finished made the fallback
            // locale - and every unlocalised spell name behind it - whichever table parsed fastest.
            for (int i = 0; i < files.Count; i++)
                tables[LocaleOf(files[i].Name)] = loaded[i];
        }

        private Task<TextTable> LoadTextTable(IProgress<EngineProgress> progress, IArchiveFile file, int total)
        {
            return Task.Run(() =>
            {
                using Stream archiveStream = file.Open();
                using var memoryStream = new MemoryStream();
                archiveStream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                var textTable = new TextTable(memoryStream);
                Interlocked.Increment(ref count);
                progress.Report(new EngineProgress(Value: count, Maximum: total));

                return textTable;
            });
        }

        public string GetText(uint id)
        {
            return Resolve()?.GetEntry(id) ?? "UNKNOWN LOCALISED TEXT ID";
        }
    }
}
