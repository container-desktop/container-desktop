namespace ContainerDesktop.Abstractions;

public abstract class VisibilityAttribute : Attribute
{
    protected VisibilityAttribute(bool show, string propertyName, object value)
    {
        Show = show;
        PropertyName = propertyName;
        Value = value;
    }

    public bool Show { get; }
    public string PropertyName { get; }
    public object Value { get; }
}
