using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.XAML.Converter;

public class UpworkStateToFillConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UpworkState state)
        {
            // When fully automated, use the original color from parameter
            if (state == UpworkState.FullyAutomated && parameter is string colorHex)
            {
                return (System.Windows.Media.Brush)new BrushConverter().ConvertFrom(colorHex)!;
            }

            return state switch
            {
                UpworkState.NoProcess => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD3, 0xD3, 0xD3)), // lightgray
                UpworkState.ProcessFoundButCannotAutomate => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xA9, 0xA9, 0xA9)), // darkgray
                UpworkState.FullyAutomated => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4b, 0x4b, 0x4b)), // fallback to dark gray
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD3, 0xD3, 0xD3))
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD3, 0xD3, 0xD3));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
