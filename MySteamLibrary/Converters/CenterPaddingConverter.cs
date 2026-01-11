using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MySteamLibrary.Converters;

public class CenterPaddingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && width > 0)
        {
            // Half of the screen minus half of your 220px card
            double centerOfScreen = width / 2;
            double halfItem = 110;

            double padding = centerOfScreen - halfItem;

            // This ensures that when ScrollOffset is 0, the first item is centered
            return new Thickness(Math.Max(0, padding), 0, Math.Max(0, padding), 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}