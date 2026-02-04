using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UpworkJiraTracker.XAML.Converter;

public class JiraConnectedToTooltipConverter : DependencyObject, IValueConverter
{
    #region Dependency Properties

    public static readonly DependencyProperty DisconnectedTextProperty =
        DependencyProperty.Register(
            nameof(DisconnectedText),
            typeof(string),
            typeof(JiraConnectedToTooltipConverter),
            new PropertyMetadata("Click to connect to Jira"));

    public static readonly DependencyProperty ConnectedTextProperty =
        DependencyProperty.Register(
            nameof(ConnectedText),
            typeof(string),
            typeof(JiraConnectedToTooltipConverter),
            new PropertyMetadata("Click to disconnect from Jira"));

    #endregion

    #region Properties

    public string DisconnectedText
    {
        get => GetValue(DisconnectedTextProperty) as string ?? "Click to connect to Jira";
        set => SetValue(DisconnectedTextProperty, value);
    }

    public string ConnectedText
    {
        get => GetValue(ConnectedTextProperty) as string ?? "Click to disconnect from Jira";
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
