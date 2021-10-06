namespace ContainerDesktop.DesiredStateConfiguration;

public interface IConfigurationManifest
{
    List<IResource> Resources { get; }

    Uri Location { get; }

    ConfigurationResult Apply(ConfigurationContext context);
}