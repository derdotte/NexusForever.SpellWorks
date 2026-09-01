using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.SpellWorks.Core.Configuration;
using NexusForever.SpellWorks.Core.Models;
using NexusForever.SpellWorks.Core.Services;
using NexusForever.SpellWorks.ViewModels;
using NexusForever.SpellWorks.Views.Windows;

namespace NexusForever.SpellWorks
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IConfiguration _configuration;
        private IServiceProvider _serviceProvider;

        public App()
        {
            var configuration = new ConfigurationBuilder();
            ConfigureConfiguration(configuration);
            _configuration = configuration.Build();

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureConfiguration(IConfigurationBuilder configuration)
        {
            configuration.AddJsonFile("Configuration.json");
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SpelllWorksConfiguration>(_configuration);

            services.AddSingleton<IMessenger, WeakReferenceMessenger>();

            services.AddTransient<MainWindow>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SpellInfoSearchViewModel>();
            services.AddTransient<SpellInfoViewModel>();
            services.AddTransient<SpellInfoSpellTabViewModel>();
            services.AddTransient<SpellInfoEffectsTabViewModel>();
            services.AddTransient<SpellInfoProcsTabViewModel>();
            services.AddTransient<MainTabViewModel>();
            services.AddTransient<SettingsTabViewModel>();

            services.AddSingleton<IDialogCoordinator, DialogCoordinator>();

            services.AddSingleton<IResourceService, ResourceService>();
            services.AddSingleton<IArchiveService, ArchiveService>();
            services.AddSingleton<ITextTableService, TextTableService>();
            services.AddSingleton<IGameTableService, GameTableService>();
            services.AddSingleton<ISpellTooltipParseService, SpellTooltipParseService>();

            services.AddSingleton<ISpellModelFilterService, SpellModelFilterService>();
            services.AddSingleton<ISpellModelService, SpellModelService>();
            services.AddTransient<ISpellModel, SpellModel>();
            services.AddTransient<ISpellBaseModel, SpellBaseModel>();
            services.AddTransient<ISpellEffectModel, SpellEffectModel>();
            services.AddTransient<ISpellProcModel, SpellProcModel>();

            foreach (Type type in typeof(ISpellEffectColumnData).Assembly.GetTypes())
            {
                var attribute = type.GetCustomAttribute<SpellEffectAttribute>();
                if (attribute == null)
                    continue;

                if (type.IsAssignableTo(typeof(ISpellEffectColumnData)))
                {
                    services.AddKeyedTransient(typeof(ISpellEffectColumnData), attribute.Type, type);
                }

                if (type.IsAssignableTo(typeof(ISpellEffectRowData)))
                {
                    services.AddKeyedTransient(typeof(ISpellEffectRowData), attribute.Type, type);
                }
            }

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetService<MainWindowViewModel>();
            mainWindow.Show();

            base.OnStartup(e);
        }
    }

}
