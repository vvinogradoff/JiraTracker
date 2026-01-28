using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using UpworkJiraTracker.Model;

namespace UpworkJiraTracker.XAML.Converter;

public class UpworkStateToFillConverter : DependencyObject, IValueConverter
{
    #region Dependency Properties

    public static readonly DependencyProperty NoProcessBrushProperty =
        DependencyProperty.Register(
            nameof(NoProcessBrush),
            typeof(Brush),
            typeof(UpworkStateToFillConverter),
            new PropertyMetadata(CreateFrozenBrush(0xD3, 0xD3, 0xD3))); // lightgray

    public static readonly DependencyProperty CannotAutomateBrushProperty =
        DependencyProperty.Register(
            nameof(CannotAutomateBrush),
            typeof(System.Windows.Media.Brush),
            typeof(UpworkStateToFillConverter),
            new PropertyMetadata(CreateFrozenBrush(0xA9, 0xA9, 0xA9))); // darkgray

    public static readonly DependencyProperty FullyAutomatedBrushProperty =
        DependencyProperty.Register(
            nameof(FullyAutomatedBrush),
            typeof(Brush),
            typeof(UpworkStateToFillConverter),
            new PropertyMetadata(CreateFrozenBrush(0x4b, 0x4b, 0x4b))); // fallback dark gray

    #endregion

    #region Properties

    public Brush NoProcessBrush
    {
        get => GetValue(NoProcessBrushProperty) as Brush ?? Brushes.LightGray;
        set => SetValue(NoProcessBrushProperty, value);
    }

    public Brush CannotAutomateBrush
    {
        get => GetValue(CannotAutomateBrushProperty) as Brush ?? Brushes.DarkGray;
        set => SetValue(CannotAutomateBrushProperty, value);
    }

    public Brush FullyAutomatedBrush
    {
        get => GetValue(FullyAutomatedBrushProperty) as Brush ?? Brushes.Black;
        set => SetValue(FullyAutomatedBrushProperty, value);
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
        if (value is UpworkState state)
        {
            // When fully automated, use the original color from parameter if provided
            if (state == UpworkState.FullyAutomated && parameter is string colorHex)
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!;
                brush.Freeze();
                return brush;
            }

            return state switch
            {
                UpworkState.NoProcess => GetFrozenBrush(NoProcessBrush),
                UpworkState.ProcessFoundButCannotAutomate => GetFrozenBrush(CannotAutomateBrush),
                UpworkState.FullyAutomated => GetFrozenBrush(FullyAutomatedBrush),
                _ => GetFrozenBrush(NoProcessBrush)
            };
        }
        return GetFrozenBrush(NoProcessBrush);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
