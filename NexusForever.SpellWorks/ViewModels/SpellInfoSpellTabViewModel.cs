using CommunityToolkit.Mvvm.ComponentModel;
using NexusForever.SpellWorks.Core.Models;

namespace NexusForever.SpellWorks.ViewModels
{
    public partial class SpellInfoSpellTabViewModel : BaseTabItem
    {
        public override string Header => "Spell";

        [ObservableProperty]
        private ISpellModel _selectedSpell;
    }
}
