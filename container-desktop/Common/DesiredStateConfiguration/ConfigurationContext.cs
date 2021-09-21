using Microsoft.Extensions.Logging;
using System;
using System.IO.Abstractions;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public class ConfigurationContext
    {
        private readonly Func<bool> _getUserConsent;

        public ConfigurationContext(ILogger logger, IFileSystem fileSystem, Func<bool> getUserConsent = null)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _getUserConsent = getUserConsent ?? (() => true);
        }

        public ILogger Logger { get; }

        public IFileSystem FileSystem { get; }

        public bool AskUserConsent()
        {
            return _getUserConsent();
        }
    }
}
