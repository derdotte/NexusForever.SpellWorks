using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using NexusForever.SpellWorks.GameTable.Static;
using NexusForever.SpellWorks.Messages;
using NexusForever.SpellWorks.Models;
using NexusForever.SpellWorks.Models.Filter;
using NexusForever.SpellWorks.Services;

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
        private bool _useFuzzy;

        partial void OnUseFuzzyChanged(bool value)
        {
            if (_filters.TryGetValue(typeof(SpellModelDescriptionFilter), out var f) && f is SpellModelDescriptionFilter descFilter)
            {
                descFilter.UseFuzzy = value;
            }

            if (value && _useIDSearch)
            {
                _useIDSearch = false;
                OnUseIDSearchChanged(false);
                OnPropertyChanged(nameof(UseIDSearch));
            }

            FilterSpells();
        }

        [ObservableProperty]
        private bool _useIDSearch;

        partial void OnUseIDSearchChanged(bool value)
        {
            if (_filters.TryGetValue(typeof(SpellModelIDFilter), out var f) && f is SpellModelIDFilter idFilter)
            {
                idFilter.useIDSearch = value;
            }

            if (value && _useFuzzy)
            {
                _useFuzzy = false;
                OnUseFuzzyChanged(false);
                OnPropertyChanged(nameof(UseFuzzy));

                _filters.Remove(typeof(SpellModelDescriptionFilter));
            }

            if (!value)
                _filters.Remove(typeof(SpellModelIDFilter));

            FilterSpells();
        }

        [ObservableProperty]
        private string _searchDescription;

        partial void OnSearchDescriptionChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _filters.Remove(typeof(SpellModelDescriptionFilter));
                _filters.Remove(typeof(SpellModelIDFilter));
                FilterSpells();
                return;
            }

            // TODO: refactor to handle toggling proerties better, for now ignore warnings
            if (_useIDSearch) 
            {
                if (!int.TryParse(value.Trim(), out int id))
                {
                    _filters.Remove(typeof(SpellModelIDFilter));
                    FilterSpells();
                    return;
                }

                _filters.Remove(typeof(SpellModelDescriptionFilter));

                if (_filters.TryGetValue(typeof(SpellModelIDFilter), out var existingIdFilter) && existingIdFilter is SpellModelIDFilter idFilter)
                {
                    idFilter.Spell4Id = id;
                    idFilter.useIDSearch = true;
                }
                else
                {
                    _filters[typeof(SpellModelIDFilter)] = new SpellModelIDFilter
                    {
                        Spell4Id = id,
                        useIDSearch = true
                    };
                }

                OnUseIDSearchChanged(_useIDSearch); // Ignore warning, binding is used with bound property
                return;
            }

            if (_filters.TryGetValue(typeof(SpellModelDescriptionFilter), out var existing) && existing is SpellModelDescriptionFilter descFilter)
            {
                descFilter.Description = value;
            }
            else
            {
                _filters[typeof(SpellModelDescriptionFilter)] = new SpellModelDescriptionFilter
                {
                    Description = value,
                    UseFuzzy = false,
                    FuzzyThreshold = 0.4 
                };
            }

            OnUseFuzzyChanged(_useFuzzy); // Ignore warning, binding is used with bound property
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
