using Microsoft.Extensions.Logging;
using System;
using System.IO.Abstractions;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public class ConfigurationContext
    {
        private readonly IUserInteraction _userInteraction;
        private readonly IApplicationContext _applicationContext;
        private readonly bool _uninstall;

        public ConfigurationContext(ILogger logger, IFileSystem fileSystem, IApplicationContext applicationContext, IUserInteraction userInteraction = null)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
            _userInteraction = userInteraction;
        }

        public bool Uninstall
        {
            get => _uninstall;
            init
            {
                _uninstall = value;
                if(_userInteraction != null)
                {
                    _userInteraction.Uninstalling = value;
                }
            }
        }

        public ILogger Logger { get; }

        public IFileSystem FileSystem { get; }

        public bool AskUserConsent(string message, string caption = null)
        {
            return _userInteraction?.UserConsent(message, caption) ?? true;
        }

        public void ReportProgress(int value, int max, string message)
        {
            _userInteraction?.ReportProgress(value, max, message);
            Logger.LogInformation(message);
        }

        public void QuitApplication()
        {
            _applicationContext.QuitApplication();
        }
    }
}
