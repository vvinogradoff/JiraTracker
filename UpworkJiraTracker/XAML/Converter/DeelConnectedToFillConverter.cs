using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UpworkJiraTracker.XAML.Converter;

public class DeelConnectedToFillConverter : DependencyObject, IValueConverter
{
    #region Dependency Properties

    public static readonly DependencyProperty DisconnectedBrushProperty =
        DependencyProperty.Register(
            nameof(DisconnectedBrush),
            typeof(Brush),
            typeof(DeelConnectedToFillConverter),
            new PropertyMetadata(CreateFrozenBrush(0xD3, 0xD3, 0xD3))); // lightgray

    public static readonly DependencyProperty ConnectedBrushProperty =
        DependencyProperty.Register(
            nameof(ConnectedBrush),
            typeof(Brush),
            typeof(DeelConnectedToFillConverter),
            new PropertyMetadata(CreateFrozenBrush(0x00, 0x00, 0x00))); // black fallback

    #endregion

    #region Properties

    public Brush DisconnectedBrush
    {
        get => GetValue(DisconnectedBrushProperty) as Brush ?? Brushes.LightGray;
        set => SetValue(DisconnectedBrushProperty, value);
    }

    public Brush ConnectedBrush
    {
        get => GetValue(ConnectedBrushProperty) as Brush ?? Brushes.Black;
        set => SetValue(ConnectedBrushProperty, value);
    }

    #endregion

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static Brush GetFrozenBrush(Brush brush)
    {
        if (brush.IsFrozen)
            return brush;

        var clone = brush.Clone();
        clone.Freeze();
        return clone;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            // When connected, use the original color from parameter if provided
            if (isConnected && parameter is string colorHex)
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!;
                brush.Freeze();
                return brush;
            }

            return isConnected
                ? GetFrozenBrush(ConnectedBrush)
                : GetFrozenBrush(DisconnectedBrush);
        }
        return GetFrozenBrush(DisconnectedBrush);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
