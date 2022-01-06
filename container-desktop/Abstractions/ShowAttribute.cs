namespace ContainerDesktop.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ShowAttribute : VisibilityAttribute
{
    public ShowAttribute(string propertyName = null, object value = null) : base(true, propertyName, value)
    {
    }
}
