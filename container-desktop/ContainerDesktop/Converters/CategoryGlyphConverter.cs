using System.Globalization;
using System.Windows.Data;

namespace ContainerDesktop.Converters;

public class CategoryGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value == null)
        {
            return Binding.DoNothing;
        }
        if (targetType == typeof(Abstractions.Symbol))
        {
            return (Abstractions.Symbol)value;
        }
        else if (targetType == typeof(ModernWpf.Controls.Symbol))
        {
            return (ModernWpf.Controls.Symbol)value;
        }
        throw new InvalidCastException($"Could not convert value '{value}' of type '{value?.GetType().FullName}' to '{targetType.FullName}'");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture);
    }
}
