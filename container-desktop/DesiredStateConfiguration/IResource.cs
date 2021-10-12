namespace ContainerDesktop.DesiredStateConfiguration;

public interface IResource
{
    string Id { get; }
    string Description { get; }
    string Type => GetTypeName(GetType());
    List<string> DependsOn { get; }
    bool Enabled { get; }
    bool RequiresReboot { get; }
    bool NoUninstall { get; }
    bool RunAllwaysFirst { get; }
    bool RunAllways { get; }
    string ExtraInformation { get; }
    bool Optional { get; }
    UpdateAction OnUpdateAction { get; }
    bool Test(ConfigurationContext context);
    void Set(ConfigurationContext context);
    void Unset(ConfigurationContext context);
    static string GetTypeName(Type type) => type.Name.EndsWith("Resource") ? type.Name[0..^8] : type.Name;
}

