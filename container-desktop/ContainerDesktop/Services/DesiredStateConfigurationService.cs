using ContainerDesktop.Common.DesiredStateConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO.Abstractions;

namespace ContainerDesktop.Services
{
    public class DesiredStateConfigurationService : IDesiredStateConfigurationService
    {
        private readonly ILogger<DesiredStateConfigurationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileSystem _fileSystem;

        public DesiredStateConfigurationService(IServiceProvider serviceProvider, ILogger<DesiredStateConfigurationService> logger, IFileSystem fileSystem)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void Apply()
        {
            var context = new ConfigurationContext(_logger, _fileSystem, AskUserConsent);
            var manifestFile = System.IO.Path.Combine(AppContext.BaseDirectory, "configuration-manifest.json");
            var configuration = Configuration.Create(_serviceProvider, manifestFile);
            configuration.Apply(context);
        }

        private bool AskUserConsent()
        {
            var result = System.Windows.Forms.MessageBox.Show(
                "Container Desktop detected that the system is not configured yet. Continue with setup ?", 
                "Container Desktop Setup", 
                System.Windows.Forms.MessageBoxButtons.YesNo);
            return result == System.Windows.Forms.DialogResult.Yes;
            //var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            //var dialog = new ContentDialog
            //{
            //    Title = "Container Desktop Setup",
            //    Content = "Container Desktop detected that the system is not configured yet. Continue with setup ?",
            //    PrimaryButtonText = "OK",
            //    CloseButtonText = "Cancel",
            //    DefaultButton = ContentDialogButton.Close,
            //    XamlRoot = mainWindow.Content.XamlRoot
            //};
            //var result = dialog.ShowAsync().GetAwaiter().GetResult();
            //return result == ContentDialogResult.Primary;
        }
    }
}
