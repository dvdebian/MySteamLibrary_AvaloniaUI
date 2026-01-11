using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MySteamLibrary.Converters;

/// <summary>
/// Converts window width to appropriate item margin.
/// Larger windows need more bottom margin because items scale larger.
/// </summary>
public class DynamicItemMarginConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && width > 0)
        {
            // Small windows: minimal margin (~20px)
            // Large windows: more margin for scaling (~80px)

            // Linear interpolation based on window width
            // 800px window -> 20px margin
            // 1920px window -> 80px margin
            double minWidth = 800;
            double maxWidth = 1920;
            double minMargin = 20;
            double maxMargin = 80;

            // Clamp width to reasonable range
            double clampedWidth = Math.Clamp(width, minWidth, maxWidth);

            // Calculate margin linearly
            double ratio = (clampedWidth - minWidth) / (maxWidth - minWidth);
            double bottomMargin = minMargin + (ratio * (maxMargin - minMargin));

            return new Thickness(5, 0, 5, bottomMargin);
        }
        return new Thickness(5, 0, 5, 20);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}