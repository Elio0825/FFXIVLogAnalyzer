using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FFXIVLogAnalyzer.Converters;

public class HighlightStateToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value switch
        {
            1 => new SolidColorBrush(Color.FromArgb(60, 76, 175, 80)),  // Highlighted green
            2 => new SolidColorBrush(Color.FromArgb(40, 158, 158, 158)), // Ignored gray
            _ => Brushes.Transparent
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class HighlightStateToDecorationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (int)value == 2 ? TextDecorations.Strikethrough : null!;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class DeltaToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var delta = (double)value;
        if (delta < 0) return new SolidColorBrush(Color.FromRgb(66, 165, 245));   // Blue for before
        if (delta > 0) return new SolidColorBrush(Color.FromRgb(255, 167, 38));   // Orange for after
        return new SolidColorBrush(Color.FromRgb(102, 187, 106));                  // Green for exact
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
