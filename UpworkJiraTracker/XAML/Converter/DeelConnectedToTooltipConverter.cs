using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UpworkJiraTracker.XAML.Converter;

public class DeelConnectedToTooltipConverter : DependencyObject, IValueConverter
{
    #region Dependency Properties

    public static readonly DependencyProperty DisconnectedTextProperty =
        DependencyProperty.Register(
            nameof(DisconnectedText),
            typeof(string),
            typeof(DeelConnectedToTooltipConverter),
            new PropertyMetadata("Deel is not connected"));

    public static readonly DependencyProperty ConnectedTextProperty =
        DependencyProperty.Register(
            nameof(ConnectedText),
            typeof(string),
            typeof(DeelConnectedToTooltipConverter),
            new PropertyMetadata("Deel is connected"));

    #endregion

    #region Properties

    public string DisconnectedText
    {
        get => GetValue(DisconnectedTextProperty) as string ?? "Deel is not connected";
        set => SetValue(DisconnectedTextProperty, value);
    }

    public string ConnectedText
    {
        get => GetValue(ConnectedTextProperty) as string ?? "Deel is connected";
        set => SetValue(ConnectedTextProperty, value);
    }

    #endregion

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? ConnectedText : DisconnectedText;
        }
        return DisconnectedText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
