namespace ContainerDesktop.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class HideAttribute : VisibilityAttribute
{
    public HideAttribute(string propertyName = null, object value = null) : base(false, propertyName, value)
    {
    }
}
