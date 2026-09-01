using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MahApps.Metro.Controls.Dialogs;
using NexusForever.SpellWorks.Core.Messages;
using NexusForever.SpellWorks.Core.Services;

namespace NexusForever.SpellWorks.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public string Title => "NexusForever SpellWorks";

        public ObservableCollection<BaseTabItem> Tabs { get; } = [];

        [ObservableProperty]
        private int _selectedTabIndex;

        public ICommand OnLoadCommand => _onLoadCommand ??= new AsyncRelayCommand(OnLoad);
        private ICommand _onLoadCommand;

        #region Dependency Injection

        private readonly IMessenger _messenger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IResourceService _resourceService;

        public MainWindowViewModel(
            IMessenger messenger,
            IDialogCoordinator dialogCoordinator,
            IResourceService resourceService,
            MainTabViewModel mainTabViewModel,
            SettingsTabViewModel settingsTabViewModel)
        {
            _messenger         = messenger;
            _dialogCoordinator = dialogCoordinator;
            _resourceService   = resourceService;

            Tabs.Add(mainTabViewModel);
            Tabs.Add(settingsTabViewModel);
        }

        #endregion

        public MainWindowViewModel()
        {
        }

        private async Task OnLoad()
        {
            ProgressDialogController controller = await _dialogCoordinator.ShowProgressAsync(this, "Loading Resources...", "");

            var progress = new Progress<EngineProgress>(report =>
            {
                if (report.Message != null)
                    controller.SetMessage(report.Message);

                controller.Minimum = report.Minimum;
                controller.Maximum = report.Maximum;

                if (report.Value is double value)
                    controller.SetProgress(value);
                else
                    controller.SetIndeterminate();
            });

            try
            {
                await _resourceService.Initialise(progress);
                _messenger.Send(new SpellResourcesLoaded());
            }
            catch (Exception e)
            {
                await _dialogCoordinator.ShowMessageAsync(this, "Error", e.Message);
            }
            finally
            {
                await controller.CloseAsync();
            }
        }
    }
}
