using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ContainerDesktop.Converters;

public class UpdateAvailableAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is bool b && b)
        {
            return HorizontalAlignment.Stretch;
        }
        return HorizontalAlignment.Center;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
