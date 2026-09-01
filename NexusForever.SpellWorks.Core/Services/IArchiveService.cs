using Nexus.Archive;

namespace NexusForever.SpellWorks.Core.Services
{
    public interface IArchiveService
    {
        Archive MainArchive { get; }
        List<Archive> LocalisationArchives { get; }

        Task Initialise();
    }
}