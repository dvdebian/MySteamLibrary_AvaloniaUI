using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MySteamLibrary.Converters;

public class CenterPaddingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double totalWidth && totalWidth > 0)
        {
            // Formula: (Full Screen Width / 2) - (Half Card Width)
            // This ensures the first card sits exactly in the middle at Offset 0
            double sidePadding = (totalWidth / 2.0) - 110.0; // 110 is half of 220
            return new Thickness(sidePadding, 0, sidePadding, 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}