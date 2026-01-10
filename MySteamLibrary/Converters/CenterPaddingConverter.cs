using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MySteamLibrary.Converters;

/// <summary>
/// Calculates the side padding needed to center an item.
/// Takes the total width and returns (Width / 2) - (ItemWidth / 2).
/// </summary>
public class CenterPaddingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            // Total width of the ListBox / 2
            double centerOfScreen = width / 2;
            // Half of our item slot (300 / 2)
            double halfItem = 150;

            double padding = centerOfScreen - halfItem;
            // Return thickness: Left and Right padding, 0 for Top and Bottom
            return new Thickness(Math.Max(0, padding), 0, Math.Max(0, padding), 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}