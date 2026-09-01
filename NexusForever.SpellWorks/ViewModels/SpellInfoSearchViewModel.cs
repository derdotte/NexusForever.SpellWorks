using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using NexusForever.Game.Static.Spell;
using NexusForever.SpellWorks.Core.Static;
using NexusForever.SpellWorks.Core.Messages;
using NexusForever.SpellWorks.Core.Models;
using NexusForever.SpellWorks.Core.Models.Filter;
using NexusForever.SpellWorks.Core.Services;

namespace NexusForever.SpellWorks.ViewModels
{
    public partial class SpellInfoSearchViewModel : ObservableObject, IRecipient<SpellResourcesLoaded>
    {
        public ObservableCollection<ISpellModel> Spells { get; } = [];

        [ObservableProperty]
        private ISpellModel _selectedSpell;

        partial void OnSelectedSpellChanged(ISpellModel value)
        {
            _messenger.Send(new SpellSelectedMessage
            {
                Spell = value
            });
        }

        public ObservableCollection<CastMethod> CastMethods { get; } = [];

        [ObservableProperty]
        private CastMethod? _castMethod;

        partial void OnCastMethodChanged(CastMethod? value)
        {
            if (value.HasValue)
            {
                _filters[typeof(SpellModelCastMethodFilter)] = new SpellModelCastMethodFilter
                {
                    CastMethod = value.Value
                };
            }
            else
                _filters.Remove(typeof(SpellModelCastMethodFilter));

            FilterSpells();
        }






        [ObservableProperty]
        private string _searchDescription;

        partial void OnSearchDescriptionChanged(string value)
        {
            /*Spells.Clear();

            var filter = new SpellModelDescriptionFilter
            {
                Description = value
            };

            foreach (var item in _spellModelFilterService.Filter([filter], _spellModelService.SpellModels))
            {
                Spells.Add(item);
            }*/
        }

        public ObservableCollection<SpellTargetMechanicType> TargetMechanicTypes { get; } = [];

        [ObservableProperty]
        private SpellTargetMechanicType? _targetMechanicType;

        partial void OnTargetMechanicTypeChanged(SpellTargetMechanicType? value)
        {
            if (value.HasValue)
            {
                _filters[typeof(SpellModelTargetMechanicTypeFilter)] = new SpellModelTargetMechanicTypeFilter
                {
                    TargetMechanicType = value.Value
                };
            }
            else
            {
                _filters.Remove(typeof(SpellModelTargetMechanicTypeFilter));
            }

            FilterSpells();
        }

        public ObservableCollection<SpellTargetMechanicFlags> TargetMechanicFlags { get; } = [];

        [ObservableProperty]
        private SpellTargetMechanicFlags? _targetMechanicFlag;

        partial void OnTargetMechanicFlagChanged(SpellTargetMechanicFlags? value)
        {
            if (value.HasValue)
            {

            }
            else
            {
            }
        }   

        



        public ObservableCollection<SpellEffectType> SpellEffectTypes { get; } = [];

        [ObservableProperty]
        private SpellEffectType? _spellEffectType;

        partial void OnSpellEffectTypeChanged(SpellEffectType? value)
        {
            if (value.HasValue)
            {
                _filters[typeof(SpellModelEffectTypeFilter)] = new SpellModelEffectTypeFilter
                {
                    SpellEffectType = value.Value
                };
            }
            else
            {
                _filters.Remove(typeof(SpellModelEffectTypeFilter));
            }

            FilterSpells();
        }




        private readonly Dictionary<Type, ISpellModelFilter> _filters = [];

        #region Dependency Injection

        private readonly IMessenger _messenger;
        private readonly ISpellModelService _spellModelService;
        private readonly ISpellModelFilterService _spellModelFilterService;

        public SpellInfoSearchViewModel(
            IMessenger messenger,
            ISpellModelService spellModelService,
            ISpellModelFilterService spellModelFilterService)
        {
            _messenger               = messenger;
            _messenger.Register<SpellResourcesLoaded>(this);

            _spellModelService       = spellModelService;
            _spellModelFilterService = spellModelFilterService;
        }

        #endregion

        public SpellInfoSearchViewModel()
        {

        }

        void IRecipient<SpellResourcesLoaded>.Receive(SpellResourcesLoaded message)
        {
            foreach (var item in Enum.GetValues<CastMethod>())
                CastMethods.Add(item);

            foreach (var item in Enum.GetValues<SpellTargetMechanicType>())
                TargetMechanicTypes.Add(item);

            foreach (var item in Enum.GetValues<SpellTargetMechanicFlags>())
                TargetMechanicFlags.Add(item);

            foreach (var item in Enum.GetValues<SpellEffectType>())
                SpellEffectTypes.Add(item);

            foreach (var item in _spellModelService.SpellModels.Values)
                Spells.Add(item);
        }

        private void FilterSpells()
        {
            Spells.Clear();
            foreach (ISpellModel model in _spellModelFilterService.Filter(_filters.Values, _spellModelService.SpellModels.Values))
                Spells.Add(model);
        }
    }
}
