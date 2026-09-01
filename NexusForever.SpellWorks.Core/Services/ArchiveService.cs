using System.IO;
using Microsoft.Extensions.Options;
using Nexus.Archive;
using NexusForever.SpellWorks.Core.Configuration;

namespace NexusForever.SpellWorks.Core.Services
{
    public class ArchiveService : IArchiveService
    {
        private static readonly string[] localisationIndexes =
        {
            "ClientDataEN.index",
            "ClientDataFR.index",
            "ClientDataDE.index"
        };

        /// <summary>
        /// Main client ClientData archive.
        /// </summary>
        public Archive MainArchive { get; private set; }

        /// <summary>
        /// Collection of client localisation archives.
        /// </summary>
        public List<Archive> LocalisationArchives { get; } = [];

        #region Dependency Injection

        private readonly SpelllWorksConfiguration _options;

        public ArchiveService(
            IOptions<SpelllWorksConfiguration> options)
        {
            _options = options.Value;
        }

        #endregion

        public Task Initialise()
        {
            // CoreData archive only applicable to Steam client
            ArchiveFile coreDataArchive = null;
            if (File.Exists(Path.Combine(_options.PatchPath, "CoreData.archive")))
                coreDataArchive = ArchiveFileBase.FromFile(Path.Combine(_options.PatchPath, "CoreData.archive")) as ArchiveFile;

            MainArchive = Archive.FromFile(Path.Combine(_options.PatchPath, "ClientData.index"), coreDataArchive);

            foreach (string localisationArchivePath in localisationIndexes
                .Select(i => Path.Combine(_options.PatchPath, i)))
            {
                if (!File.Exists(localisationArchivePath))
                    continue;

                LocalisationArchives.Add(Archive.FromFile(localisationArchivePath, coreDataArchive));
            }

            return Task.CompletedTask;
        }
    }
}
