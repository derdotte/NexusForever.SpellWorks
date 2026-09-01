using System.IO;
using Nexus.Archive;
using NexusForever.GameTable;

namespace NexusForever.SpellWorks.Core.Services
{
    public class TextTableService : ITextTableService
    {
        private TextTable currentTextTable;

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
            int total = _archiveService.LocalisationArchives.Count;
            progress.Report(new EngineProgress("Loading Text Tables...", 0, 0, total));

            List<Task> tasks = [];
            foreach (Archive archive in _archiveService.LocalisationArchives)
            {
                foreach (IArchiveFileEntry file in archive.IndexFile.GetFiles("*.bin"))
                {
                    tasks.Add(LoadTextTable(progress, archive, file, total));
                }
            }

            await Task.WhenAll(tasks);
        }

        private Task<TextTable> LoadTextTable(IProgress<EngineProgress> progress, Archive archive, IArchiveFileEntry file, int total)
        {
            return Task.Run(() =>
            {
                using Stream archiveStream = archive.OpenFileStream(file);
                using var memoryStream = new MemoryStream();
                archiveStream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                var textTable = new TextTable(memoryStream);
                Interlocked.Increment(ref count);
                progress.Report(new EngineProgress(Value: count, Maximum: total));

                // TODO: fix me
                currentTextTable = textTable;
                return textTable;
            });
        }

        public string GetText(uint id)
        {
            return currentTextTable.GetEntry(id) ?? "UNKNOWN LOCALISED TEXT ID";
        }
    }
}
