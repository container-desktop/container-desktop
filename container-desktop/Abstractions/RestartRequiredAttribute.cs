namespace ContainerDesktop.Abstractions
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RestartRequiredAttribute : Attribute
    {
    }
}
