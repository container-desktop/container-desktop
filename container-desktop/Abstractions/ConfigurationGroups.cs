namespace ContainerDesktop.Abstractions;

public static class ConfigurationGroups
{
    public const string Network = nameof(Network);
    public const string Miscellaneous = nameof(Miscellaneous);

    private static readonly string[] _groupOrder = new[] { Network, Miscellaneous };


    public static int GetGroupOrder(string groupName)
    {
        var index = Array.IndexOf(_groupOrder, groupName);
        if (index >= 0)
        {
            return index;
        }
        return int.MaxValue;
    }
}
