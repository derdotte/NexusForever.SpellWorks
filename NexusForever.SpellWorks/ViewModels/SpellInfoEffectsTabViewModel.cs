using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NexusForever.SpellWorks.Core.Messages;
using NexusForever.SpellWorks.Core.Models;
using NexusForever.SpellWorks.Core.Services;

namespace NexusForever.SpellWorks.ViewModels
{
    public partial class SpellInfoEffectsTabViewModel : BaseTabItem
    {
        public override string Header => "Effects";

        [ObservableProperty]
        private ISpellModel _selectedSpell;

        partial void OnSelectedSpellChanged(ISpellModel value)
        {
            Effects.Clear();
            if (value == null)
                return;

            foreach (ISpellEffectModel effect in value.Effects)
                Effects.Add(effect);
        }

        public ICommand SpellHyperLinkCommand => _spellHyperlinkCommand ??= new RelayCommand<string>(OnSpellHyperLink);
        private ICommand _spellHyperlinkCommand;

        public ObservableCollection<ISpellEffectModel> Effects { get; } = [];

        #region Dependency Injection

        private IMessenger _messenger;
        private ISpellModelService _spellModelService;

        public SpellInfoEffectsTabViewModel(
            IMessenger messenger,
            ISpellModelService spellModelService)
        {
            _messenger         = messenger;
            _spellModelService = spellModelService;
        }

        #endregion

        public SpellInfoEffectsTabViewModel()
        {
        }

        private void OnSpellHyperLink(string value)
        {
            if (!_spellModelService.SpellModels.TryGetValue(uint.Parse(value), out ISpellModel model))
                return;

            _messenger.Send(new SpellHyperlinkClicked
            {
                Spell = model
            });
        }
    }
}
