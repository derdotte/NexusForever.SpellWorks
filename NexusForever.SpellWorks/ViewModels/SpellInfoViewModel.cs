using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NexusForever.SpellWorks.Core.Messages;
using NexusForever.SpellWorks.Core.Models;

namespace NexusForever.SpellWorks.ViewModels
{
    public partial class SpellInfoViewModel : ObservableObject, IRecipient<SpellSelectedMessage>, IRecipient<SpellHyperlinkClicked>
    {
        public ObservableCollection<BaseTabItem> Tabs { get; } = [];

        private SpellInfoSpellTabViewModel _spellInfoSpellTabViewModel;
        private SpellInfoEffectsTabViewModel _spellInfoEffectsTabViewModel;
        private SpellInfoProcsTabViewModel _spellInfoProcsTabViewModel;

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private ISpellModel _selectedSpell;

        partial void OnSelectedSpellChanged(ISpellModel value)
        {
            _spellInfoSpellTabViewModel.SelectedSpell = value;
            _spellInfoEffectsTabViewModel.SelectedSpell = value;
            _spellInfoProcsTabViewModel.SelectedSpell = value;

            Title = $"{value?.Id} - {value?.Description}";
        }

        [ObservableProperty]
        private bool _isLocked;

        public ICommand BackButtonCommand => _backButtonCommand ??= new RelayCommand(OnBackButtonCommand);
        private ICommand _backButtonCommand;

        private void OnBackButtonCommand()
        {
            SelectedSpell = _history.Pop();
        }

        [ObservableProperty]
        private bool _backButtonIsEnabled;

        private Stack<ISpellModel> _history = [];



        #region Depedency Injection

        public SpellInfoViewModel(
            IMessenger messenger,
            SpellInfoSpellTabViewModel spellInfoSpellTabViewModel,
            SpellInfoEffectsTabViewModel spellInfoEffectsTabViewModel,
            SpellInfoProcsTabViewModel spellInfoProcsTabViewModel)
        {
            messenger.Register<SpellSelectedMessage>(this);
            messenger.Register<SpellHyperlinkClicked>(this);

            _spellInfoSpellTabViewModel   = spellInfoSpellTabViewModel;
            _spellInfoEffectsTabViewModel = spellInfoEffectsTabViewModel;
            _spellInfoProcsTabViewModel   = spellInfoProcsTabViewModel;


            Tabs.Add(_spellInfoSpellTabViewModel);
            Tabs.Add(_spellInfoEffectsTabViewModel);
            Tabs.Add(_spellInfoProcsTabViewModel);
        }

        #endregion

        public SpellInfoViewModel()
        {
        }

        public void Receive(SpellSelectedMessage message)
        {
            OnBla(message.Spell);
        }

        public void Receive(SpellHyperlinkClicked message)
        {
            OnBla(message.Spell);
        }

        public void OnBla(ISpellModel spell)
        {
            if (IsLocked)
                return;

            SelectedSpell = spell;
        }


        
    }
}
