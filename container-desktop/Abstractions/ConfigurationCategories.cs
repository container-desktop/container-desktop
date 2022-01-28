namespace ContainerDesktop.Abstractions;

public static class ConfigurationCategories
{
    public const string Basic = nameof(Basic);
    public const string Advanced = nameof(Advanced);

    public static IReadOnlyCollection<string> All { get; } = new string[] { Basic, Advanced };
}
