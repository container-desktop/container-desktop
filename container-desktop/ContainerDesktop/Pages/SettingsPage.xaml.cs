using ContainerDesktop.Abstractions;
using ContainerDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace ContainerDesktop.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
