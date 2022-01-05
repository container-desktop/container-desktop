using ContainerDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ContainerDesktop.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    private readonly IServiceProvider _serviceProvider;

    public SettingsPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        InitializeComponent();
        DataContext = ViewModel = serviceProvider.GetRequiredService<SettingsViewModel>();
    }

    public SettingsViewModel ViewModel { get; }

    private void NavigationViewLoaded(object sender, RoutedEventArgs e)
    {

    }

    private void NavigationViewSelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
    {
    }

    private void NavigationViewBackRequested(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewBackRequestedEventArgs args)
    {
        var mainPage = _serviceProvider.GetRequiredService<MainPage>();
        NavigationService.Navigate(mainPage);
    }
}
