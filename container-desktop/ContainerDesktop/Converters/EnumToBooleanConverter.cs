using System.Globalization;
using System.Windows.Data;

namespace ContainerDesktop.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value == null || !value.GetType().IsEnum)
        { 
            throw new InvalidOperationException("Target type must be an enum type.");
        }
        return value == parameter;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var b = (bool)value;
        if(b)
        {
            return parameter;
        }
        return Binding.DoNothing;
    }
}
