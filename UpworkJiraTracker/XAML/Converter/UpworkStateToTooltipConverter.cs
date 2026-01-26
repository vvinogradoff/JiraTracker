using System.Globalization;
using System.Windows.Data;
using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.XAML.Converter;

public class UpworkStateToTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UpworkState state)
        {
            return state switch
            {
                UpworkState.NoProcess => "No upwork process was found",
                UpworkState.ProcessFoundButCannotAutomate => Constants.Upwork.UpworkStartTooltip,
                UpworkState.FullyAutomated => "Upwork is connected and automated",
                _ => "Unknown state"
            };
        }
        return "Unknown state";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
