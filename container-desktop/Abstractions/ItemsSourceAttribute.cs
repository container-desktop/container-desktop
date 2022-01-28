namespace ContainerDesktop.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ItemsSourceAttribute : Attribute
{
    public ItemsSourceAttribute(string methodName)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    public string MethodName { get; }
}
