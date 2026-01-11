using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MySteamLibrary.Converters;

/// <summary>
/// Converts window height to appropriate ScrollViewer top margin.
/// Larger windows need more negative margin to pull content up.
/// </summary>
public class DynamicScrollViewerMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double height && height > 0)
        {
            // Small windows: less negative margin (-100px)
            // Large windows: much more negative margin to pull up (-250px)

            // Linear interpolation based on window height
            // 600px window -> -100px margin
            // 1080px window -> -250px margin
            double minHeight = 600;
            double maxHeight = 1080;
            double minMargin = -100;
            double maxMargin = -250;

            // Clamp height to reasonable range
            double clampedHeight = Math.Clamp(height, minHeight, maxHeight);

            // Calculate margin linearly
            double ratio = (clampedHeight - minHeight) / (maxHeight - minHeight);
            double topMargin = minMargin + (ratio * (maxMargin - minMargin));

            return new Thickness(0, topMargin, 0, 0);
        }
        return new Thickness(0, -100, 0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}