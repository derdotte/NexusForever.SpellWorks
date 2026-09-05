using Microsoft.Extensions.DependencyInjection;
using NexusForever.GameTable.Model;
using NexusForever.Game.Static.Spell;
using NexusForever.SpellWorks.Core.Models;

namespace NexusForever.SpellWorks.Core.Services
{
    public class SpellModelService : ISpellModelService
    {
        public Dictionary<uint, ISpellBaseModel> SpellBaseModels { get; } = [];
        public Dictionary<uint, ISpellModel> SpellModels { get; } = [];
        public Dictionary<uint, List<ISpellEffectModel>> SpellEffectModels { get; } = [];
        public Dictionary<uint, List<ISpellProcModel>> SpellProcModels { get; } = [];
        public Dictionary<uint, List<uint>> SpellProcReferences { get; } = [];
        public Dictionary<SpellEffectType, EffectTypeUsage> EffectTypeUsages { get; } = [];

        #region Dependency Injection

        private readonly IGameTableService _gameTableService;
        private readonly IServiceProvider _serviceProvider;

        public SpellModelService(
            IGameTableService gameTableService,
            IServiceProvider serviceProvider)
        {
            _gameTableService = gameTableService;
            _serviceProvider = serviceProvider;
        }

        #endregion

        /// <summary>
        /// Clear every model dictionary ahead of a reload. The dictionaries themselves are never reassigned.
        /// </summary>
        public void Reset()
        {
            SpellBaseModels.Clear();
            SpellModels.Clear();
            SpellEffectModels.Clear();
            SpellProcModels.Clear();
            SpellProcReferences.Clear();
            EffectTypeUsages.Clear();
        }

        public Task Initialise(IProgress<EngineProgress> progress)
        {
            progress.Report(new EngineProgress("Loading Spell Models..."));

            Reset();

            InitialiseBaseSpellModels();
            InitialiseSpellEffectModels();
            InitialiseSpellProcsModels();

            // must happen last, requires effects and procs to be initialised
            InitialiseSpells();

            return Task.CompletedTask;
        }

        private void InitialiseBaseSpellModels()
        {
            foreach (Spell4BaseEntry item in _gameTableService.Spell4Base.Entries)
            {
                var model = _serviceProvider.GetService<ISpellBaseModel>();
                model.Initialise(item);
                SpellBaseModels.Add(model.Entry.Id, model);
            }
        }

        private void InitialiseSpells()
        {
            foreach (Spell4Entry item in _gameTableService.Spell4.Entries)
            {
                var model = _serviceProvider.GetService<ISpellModel>();
                model.Initialise(item);
                SpellModels.Add(item.Id, model);
            }
        }

        /// <summary>
        /// Build the per-spell effect lists and, in the same pass, the reverse index from effect type back to
        /// the spells using it. There are over 100k effect rows, so the reverse index rides along here rather than
        /// walking the table a second time.
        /// </summary>
        private void InitialiseSpellEffectModels()
        {
            // Spell ids are collected in a set because a spell may carry several effects of one type and must
            // still count once; the row tally alongside it is what counts them all.
            Dictionary<SpellEffectType, (HashSet<uint> Spells, int Rows)> usage = [];

            foreach (var spellEffectsBySpellId in _gameTableService.Spell4Effects.Entries
                .GroupBy(e => e.SpellId))
            {
                var effectList = new List<ISpellEffectModel>();
                SpellEffectModels.Add(spellEffectsBySpellId.Key, effectList);

                foreach (Spell4EffectsEntry entry in spellEffectsBySpellId)
                {
                    var model = _serviceProvider.GetService<ISpellEffectModel>();
                    model.Initialise(entry);
                    effectList.Add(model);

                    if (!usage.TryGetValue(entry.EffectType, out (HashSet<uint> Spells, int Rows) counts))
                        counts = ([], 0);

                    counts.Spells.Add(entry.SpellId);
                    usage[entry.EffectType] = (counts.Spells, counts.Rows + 1);
                }
            }

            foreach ((SpellEffectType type, (HashSet<uint> spells, int rows)) in usage)
            {
                EffectTypeUsages.Add(type, new EffectTypeUsage
                {
                    Type           = type,
                    SpellIds       = [.. spells.Order()],
                    EffectRowCount = rows
                });
            }
        }

        private void InitialiseSpellProcsModels()
        {
            foreach (var spellEffectsBySpellId in _gameTableService.Spell4Effects.Entries
                .GroupBy(e => e.SpellId))
            {
                var procsList = new List<ISpellProcModel>();
                SpellProcModels.Add(spellEffectsBySpellId.Key, procsList);

                foreach (Spell4EffectsEntry spellEffectEntry in spellEffectsBySpellId
                    .Where(e => e.EffectType == SpellEffectType.Proc))
                {
                    var procModel = _serviceProvider.GetService<ISpellProcModel>();
                    procModel.Initialise(spellEffectEntry);
                    procsList.Add(procModel);

                    if (!SpellProcReferences.TryGetValue(spellEffectEntry.DataBits01, out List<uint> references))
                    {
                        references = new List<uint>();
                        SpellProcReferences.Add(spellEffectEntry.DataBits01, references);
                    }

                    references.Add(spellEffectEntry.SpellId);
                }
                
            }
        }
    }
}
