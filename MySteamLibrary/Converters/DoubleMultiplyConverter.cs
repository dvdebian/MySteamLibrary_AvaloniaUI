using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MySteamLibrary.Converters;

/// <summary>
/// Multiplies a double value by a parameter (percentage).
/// Used to calculate dynamic heights for the UI overlay.
/// </summary>
public class DoubleMultiplyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Ensure we have a valid number and a multiplier parameter
        if (value is double baseValue && double.TryParse(parameter?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double multiplier))
        {
            return baseValue * multiplier;
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}