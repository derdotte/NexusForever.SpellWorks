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
    public partial class SpellInfoProcsTabViewModel : BaseTabItem
    {
        public override string Header => "Procs";

        [ObservableProperty]
        private ISpellModel _selectedSpell;

        partial void OnSelectedSpellChanged(ISpellModel value)
        {
            Procs.Clear();
            ProcReferences.Clear();

            if (value == null)
                return;

            foreach (ISpellProcModel proc in value.Procs)
                Procs.Add(proc);

            foreach (uint spellId in value.ProcReferences)
                ProcReferences.Add(spellId);
        }

        public ICommand SpellHyperLinkCommand => _spellHyperlinkCommand ??= new RelayCommand<uint>(OnSpellHyperLink);
        private ICommand _spellHyperlinkCommand;

        public ObservableCollection<ISpellProcModel> Procs { get; } = [];
        public ObservableCollection<uint> ProcReferences { get; } = [];

        #region

        private IMessenger _messenger;
        private ISpellModelService _spellModelService;

        public SpellInfoProcsTabViewModel(
            IMessenger messenger,
            ISpellModelService spellModelService)
        {
            _messenger         = messenger;
            _spellModelService = spellModelService;
        }

        #endregion

        public SpellInfoProcsTabViewModel()
        {
        }

        private void OnSpellHyperLink(uint value)
        {
            if (!_spellModelService.SpellModels.TryGetValue(value, out ISpellModel model))
                return;

            _messenger.Send(new SpellHyperlinkClicked
            {
                Spell = model
            });
        }
    }
}
