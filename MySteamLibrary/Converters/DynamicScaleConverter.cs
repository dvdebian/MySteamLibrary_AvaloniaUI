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
            // values[1] = IsSelected or IsRawValue (bool)
            if (values.Count >= 2 && values[0] is double width && values[1] is bool isSelected)
            {
                // Calculate the scale factor based on your formula
                double baseScale = 0.5;
                double divisor = 2000;
                double calculatedScale = baseScale + (width / divisor);

                // Clamp the scale between 1.1 and 3.0 as per your requirements
                double finalScale = width <= 0 ? 1.1 : Math.Clamp(calculatedScale, 1.1, 3.0);

                // If the ListBox is asking for its total Height (using the ConverterParameter)
                if (parameter?.ToString() == "GetHeight")
                {
                    // Base card height is 330.
                    // We calculate the scaled height and add a 15% buffer for the selection zoom effect and shadows.
                    double baseHeight = 330;
                    double buffer = 1.15;
                    return baseHeight * finalScale * buffer;
                }

                // Standard behavior: If not selected, keep standard size (1.0)
                if (!isSelected) return 1.0;

                return finalScale;
            }

            return 1.0;
        }
    }
}