using ContainerDesktop.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ContainerDesktop.Pages;

/// <summary>
/// Interaction logic for MainPage.xaml
/// </summary>
public partial class MainPage : Page
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SettingsPage _settingsPage;

    public MainPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        InitializeComponent();
        _settingsPage = _serviceProvider.GetRequiredService<SettingsPage>();
        navigationView.IsSettingsVisible = _settingsPage != null;
    }

    private void NavigationViewLoaded(object sender, RoutedEventArgs e)
    {

    }

    private void NavigationViewSelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigationService.Navigate(_settingsPage);
        }
        else if(args.SelectedItem is Category category && category.PageType != null)
        {
            var page = _serviceProvider.GetRequiredService(category.PageType);
            contentFrame.Navigate(page);
        }
    }
}
