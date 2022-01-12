using System.Globalization;
using System.Windows.Data;

namespace ContainerDesktop.Converters;

public class UpdateIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is bool b && b && parameter is string s)
        {
            return App.Current.TryFindResource(s);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
