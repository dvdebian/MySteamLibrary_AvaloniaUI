using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Transformation; // Required for TransformOperations
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MySteamLibrary.Converters
{
    public class DynamicScaleConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // Safeguard: Check for UnsetValue to prevent crashes during initialization
            if (values.Count < 2 || values[0] == AvaloniaProperty.UnsetValue || values[1] == AvaloniaProperty.UnsetValue)
            {
                return GetDefaultReturn(parameter);
            }

            if (values[0] is double width && values[1] is bool isSelected)
            {
                // Dynamic scale calculation based on window width
                double baseScale = 0.5;
                double divisor = 2000;
                double calculatedScale = baseScale + (width / divisor);
                double finalScale = width <= 0 ? 1.1 : Math.Clamp(calculatedScale, 1.1, 3.0);

                // Handle ListBox Height request
                if (parameter?.ToString() == "GetHeight")
                {
                    double baseHeight = 330;
                    double buffer = 1.17;
                    return baseHeight * finalScale * buffer;
                }

                // Handle RenderTransform request
                // We return a TransformOperations object instead of a string to avoid Cast Exceptions
                if (!isSelected)
                {
                    return TransformOperations.Parse("scale(1.0)");
                }

                string scaleString = string.Format(CultureInfo.InvariantCulture, "scale({0:F3})", finalScale);
                return TransformOperations.Parse(scaleString);
            }

            return GetDefaultReturn(parameter);
        }

        private object GetDefaultReturn(object? parameter)
        {
            if (parameter?.ToString() == "GetHeight")
            {
                return 400.0;
            }
            // Ensure the default return matches the expected object type
            return TransformOperations.Parse("scale(1.0)");
        }
    }
}