namespace ContainerDesktop.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ItemsSourceAttribute : Attribute
{
    public ItemsSourceAttribute(string methodName, Type itemType)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
    }

    public string MethodName { get; }

    public bool Refreshable { get; init; }

    public Type ItemType { get; }
}
