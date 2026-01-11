using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using MySteamLibrary.Models;

namespace MySteamLibrary.Converters;

/// <summary>
/// Converter that returns a high ZIndex (100) for the selected item, and low ZIndex (1) for others.
/// This ensures the selected/centered game overlaps adjacent games.
/// </summary>
public class SelectedZIndexConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is GameModel currentGame && values[1] is GameModel selectedGame)
        {
            // Selected game gets highest ZIndex to appear on top
            return currentGame == selectedGame ? 100 : 1;
        }

        return 1; // Default ZIndex
    }
}