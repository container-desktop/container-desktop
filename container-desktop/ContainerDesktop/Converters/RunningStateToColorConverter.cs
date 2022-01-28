using ContainerDesktop.Services;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ContainerDesktop.Converters;

public class RunningStateToColorConverter : IValueConverter
{
    private static readonly Brush RedBrush = new SolidColorBrush(Color.FromRgb(242, 80, 34));
    private static readonly Brush GreenBrush = new SolidColorBrush(Color.FromRgb(127, 186, 0));
    private static readonly Brush OrangeBrush = new SolidColorBrush(Color.FromRgb(255, 185, 0));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is RunningState s)
        {
            return s switch
            {
                RunningState.Running => GreenBrush,
                RunningState.Stopped => RedBrush,
                _ => OrangeBrush
            };
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
