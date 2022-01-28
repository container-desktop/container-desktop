namespace ContainerDesktop.Abstractions;

public class Category : IMenuItem
{
    public string Name { get; set; }
    public string Tooltip { get; set; }
    public Symbol? Glyph { get; set; }
    public ConfigurationObject SettingsObject { get; set; }
    public Type PageType { get; set; }
}
