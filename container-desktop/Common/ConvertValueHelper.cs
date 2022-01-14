namespace ContainerDesktop.Common;

public static class ConvertValueHelper
{
    public static T ConvertFrom<T>(string value)
    {
        var paramType = typeof(T);
        return (T) ConvertFrom(paramType, value);
    }

    public static object ConvertFrom(Type targetType, string value)
    {
        var converter = TypeDescriptor.GetConverter(targetType);
        return converter?.CanConvertFrom(typeof(string)) == true ? converter.ConvertFrom(value) : Convert.ChangeType(value, targetType);
    }
}
