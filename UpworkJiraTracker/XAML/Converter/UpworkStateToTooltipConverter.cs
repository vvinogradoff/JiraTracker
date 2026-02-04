using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.XAML.Converter;

public class UpworkStateToTooltipConverter : DependencyObject, IValueConverter
{
    #region Dependency Properties

    public static readonly DependencyProperty NoProcessTextProperty =
        DependencyProperty.Register(
            nameof(NoProcessText),
            typeof(string),
            typeof(UpworkStateToTooltipConverter),
            new PropertyMetadata("No upwork process was found"));

    public static readonly DependencyProperty CannotAutomateTextProperty =
        DependencyProperty.Register(
            nameof(CannotAutomateText),
            typeof(string),
            typeof(UpworkStateToTooltipConverter),
            new PropertyMetadata("Upwork desktop tracker application must be start with --enable-features=UiaProvider"));

    public static readonly DependencyProperty FullyAutomatedTextProperty =
        DependencyProperty.Register(
            nameof(FullyAutomatedText),
            typeof(string),
            typeof(UpworkStateToTooltipConverter),
            new PropertyMetadata("Upwork is connected and automated"));

    public static readonly DependencyProperty UnknownStateTextProperty =
        DependencyProperty.Register(
            nameof(UnknownStateText),
            typeof(string),
            typeof(UpworkStateToTooltipConverter),
            new PropertyMetadata("Unknown state"));

    #endregion

    #region Properties

    public string NoProcessText
    {
        get => GetValue(NoProcessTextProperty) as string ?? "No upwork process was found";
        set => SetValue(NoProcessTextProperty, value);
    }

    public string CannotAutomateText
    {
        get => GetValue(CannotAutomateTextProperty) as string ?? "Upwork desktop tracker application must be start with --enable-features=UiaProvider";
        set => SetValue(CannotAutomateTextProperty, value);
    }

    public string FullyAutomatedText
    {
        get => GetValue(FullyAutomatedTextProperty) as string ?? "Upwork is connected and automated";
        set => SetValue(FullyAutomatedTextProperty, value);
    }

    public string UnknownStateText
    {
        get => GetValue(UnknownStateTextProperty) as string ?? "Unknown state";
        set => SetValue(UnknownStateTextProperty, value);
    }

    #endregion

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UpworkState state)
        {
            return state switch
            {
                UpworkState.NoProcess => NoProcessText,
                UpworkState.ProcessFoundButCannotAutomate => CannotAutomateText,
                UpworkState.FullyAutomated => FullyAutomatedText,
                _ => UnknownStateText
            };
        }
        return UnknownStateText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
