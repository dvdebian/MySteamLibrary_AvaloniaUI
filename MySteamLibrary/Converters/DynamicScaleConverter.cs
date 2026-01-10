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
            // values[0] = Width (double)
            // values[1] = IsSelected (bool)
            if (values.Count >= 2 && values[0] is double width && values[1] is bool isSelected)
            {
                // If not selected, keep standard size (1.0)
                if (!isSelected) return 1.0;

                if (width <= 0) return 1.1;

                // 1. Lowered the base from 0.8 to 0.5 for a smaller default zoom.
                // 2. Kept the divisor at 2000 to maintain steady growth.
                // Formula: 0.5 + (1920 / 2000) = ~1.46x zoom at 1080p.
                double scaleFactor = 0.5 + (width / 2000);

                // Ensure it never goes below 1.1 and never above 3.0
                return Math.Clamp(scaleFactor, 1.1, 3.0);
            }

            return 1.0;
        }
    }
}