using System;

namespace ContainerDesktop.Common.DesiredStateConfiguration
{
    public class PackedConfigurationManifest : ConfigurationManifest
    {
        public PackedConfigurationManifest(Uri packUri, IServiceProvider serviceProvider)
            : base(serviceProvider, ResourceUtilities.GetPackContent(packUri), packUri)
        {
        }
    }
}
