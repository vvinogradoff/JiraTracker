using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UpworkJiraTracker.XAML.Converter;

public class JiraConnectedToFillConverter : DependencyObject, IValueConverter
{
    #region Dependency Properties

    public static readonly DependencyProperty DisconnectedBrushProperty =
        DependencyProperty.Register(
            nameof(DisconnectedBrush),
            typeof(Brush),
            typeof(JiraConnectedToFillConverter),
            new PropertyMetadata(new SolidColorBrush(Colors.Gray)));

    public static readonly DependencyProperty ConnectedBrushProperty =
        DependencyProperty.Register(
            nameof(ConnectedBrush),
            typeof(Brush),
            typeof(JiraConnectedToFillConverter),
            new PropertyMetadata(new SolidColorBrush(Colors.Green)));

    #endregion

    #region Properties

    public Brush DisconnectedBrush
    {
        get => (Brush)GetValue(DisconnectedBrushProperty);
        set => SetValue(DisconnectedBrushProperty, value);
    }

    public Brush ConnectedBrush
    {
        get => (Brush)GetValue(ConnectedBrushProperty);
        set => SetValue(ConnectedBrushProperty, value);
    }

    #endregion

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            // If connected and a color parameter is provided, use that color
            if (isConnected && parameter is string colorHex)
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!;
                brush.Freeze();
                return brush;
            }

            // Otherwise use the brush properties
            return isConnected ? GetFrozenBrush(ConnectedBrush) : GetFrozenBrush(DisconnectedBrush);
        }

        return GetFrozenBrush(DisconnectedBrush);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Brush GetFrozenBrush(Brush brush)
    {
        if (brush.IsFrozen)
            return brush;

        var clonedBrush = brush.Clone();
        clonedBrush.Freeze();
        return clonedBrush;
    }
}
