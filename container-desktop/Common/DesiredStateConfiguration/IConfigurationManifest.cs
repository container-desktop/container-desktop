using System;
using System.Collections.Generic;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public interface IConfigurationManifest
    {
        List<IResource> Resources { get; }

        Uri Location { get; }

        void Apply(ConfigurationContext context);
    }
}
