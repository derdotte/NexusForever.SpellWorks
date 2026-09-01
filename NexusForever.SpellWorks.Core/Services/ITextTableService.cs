namespace NexusForever.SpellWorks.Core.Services
{
    public interface ITextTableService
    {
        Task Initialise(IProgress<EngineProgress> progress);

        string GetText(uint id);
    }
}