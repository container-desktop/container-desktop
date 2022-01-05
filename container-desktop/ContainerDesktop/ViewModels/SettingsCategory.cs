namespace ContainerDesktop.ViewModels;

public class SettingsCategory
{
    public SettingsCategory(string name, IEnumerable<SettingsProperty> properties)
    {
        Name = name;
        Properties = properties;
    }

    public string Name { get; }
    public IEnumerable<SettingsProperty> Properties { get; }
}
