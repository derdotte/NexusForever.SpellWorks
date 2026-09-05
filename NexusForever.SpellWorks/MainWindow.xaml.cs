using System.Windows;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using NexusForever.SpellWorks.Components;
using NexusForever.SpellWorks.Services;

namespace NexusForever.SpellWorks
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class MainWindow : Window
    {
        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            WindowWorkArea.Attach(this);

            WebView.Services = serviceProvider;
            WebView.RootComponents.Add(new RootComponent
            {
                Selector      = "#app",
                ComponentType = typeof(Shell),
                Parameters    = new Dictionary<string, object>
                {
                    [nameof(Shell.Bridge)] = new WindowBridge(this)
                }
            });
        }
    }
}
