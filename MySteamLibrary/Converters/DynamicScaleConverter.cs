using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MySteamLibrary.Converters
{
    public class DynamicScaleConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // values[0] = UserControl Width
            // values[1] = ListBoxItem.IsSelected (bool)
            if (values.Count >= 2 && values[0] is double width && values[1] is bool isSelected)
            {
                if (!isSelected) return 1.0; // Don't zoom unselected items

                // Calculate zoom factor
                double scaleFactor = 1.0 + (width / 5000) + 0.1;
                return Math.Clamp(scaleFactor, 1.15, 1.5);
            }

            return 1.0;
        }
    }
}