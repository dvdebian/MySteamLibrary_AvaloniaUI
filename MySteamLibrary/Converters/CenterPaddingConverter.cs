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
            double centerOfScreen = width / 2;
            double halfItem = 110; // Half of 220px item width

            double padding = centerOfScreen - halfItem;

            return new Thickness(Math.Max(0, padding), 0, Math.Max(0, padding), 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}