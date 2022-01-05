using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace ContainerDesktop.Converters;

public class EnumNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var type = value?.GetType();
        if (type?.IsEnum == true)
        {
            var name = Enum.GetName(type, value);
            var field = type.GetField(name);
            return field.GetCustomAttribute<DisplayAttribute>()?.Name ?? name;
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
