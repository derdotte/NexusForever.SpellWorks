using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.GameTable.Model;
using NexusForever.Game.Static.Spell;
using NexusForever.SpellWorks.Models;

namespace NexusForever.SpellWorks.Services
{
    public class SpellModelService : ISpellModelService
    {
        public Dictionary<uint, ISpellBaseModel> SpellBaseModels { get; } = [];
        public Dictionary<uint, ISpellModel> SpellModels { get; } = [];
        public Dictionary<uint, List<ISpellEffectModel>> SpellEffectModels { get; } = [];
        public Dictionary<uint, List<ISpellProcModel>> SpellProcModels { get; } = [];
        public Dictionary<uint, List<uint>> SpellProcReferences { get; } = [];

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

        public Task Initialise(ProgressDialogController controller)
        {
            controller.SetIndeterminate();
            controller.SetMessage("Loading Spell Models...");

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

        private void InitialiseSpellEffectModels()
        {
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
                }
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
