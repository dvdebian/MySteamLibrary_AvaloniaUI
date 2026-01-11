using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Transformation;
using MySteamLibrary.Models;

namespace MySteamLibrary.Converters;

public enum CarouselEffect
{
    ModernStack,
    InvertedV,
    ModernStackArc,
    FlatZoom,
    ConsoleShelf,
    DeepSpiral,
    Wave,
    CardsOnTable,
    Skyline
}

public class CarouselTransformConverter : IMultiValueConverter
{
    // CHANGE THIS TO TEST DIFFERENT LOOKS
    private const CarouselEffect CurrentMode = CarouselEffect.InvertedV;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // 1. Get window width for adaptive scaling
        double windowWidth = 1920.0;
        if (values.Count > 3)
        {
            if (values[3] is Rect rect) windowWidth = rect.Width;
            else if (values[3] is double d) windowWidth = d;
        }
        double adaptiveBase = Math.Clamp(windowWidth / 1920.0, 0.75, 1.1);

        if (parameter?.ToString() == "GetHeight")
        {
            bool needsExtraHeight = CurrentMode == CarouselEffect.ModernStackArc || CurrentMode == CarouselEffect.Wave;
            return 330 * (needsExtraHeight ? 1.8 : 1.6) * adaptiveBase;
        }

        double scaleX = 1.0, scaleY = 1.0, skewY = 0.0, translateX = 0.0, translateY = 0.0;

        // values[0]: Current Item (GameModel)
        // values[1]: Selected Item (GameModel)
        // values[2]: The Games Collection (IEnumerable)
        if (values.Count >= 3 && values[0] is GameModel currentItem && values[2] is IEnumerable collection)
        {
            var selectedItem = values[1] as GameModel;

            // Cast collection to list once for indexing
            var list = collection as IList ?? new List<object>();

            int currentIndex = -1;
            int selectedIndex = -1;

            // Find indices
            int i = 0;
            foreach (var item in list)
            {
                if (item == currentItem) currentIndex = i;
                if (item == selectedItem) selectedIndex = i;
                if (currentIndex != -1 && selectedIndex != -1) break;
                i++;
            }

            if (currentIndex != -1 && selectedIndex != -1)
            {
                int diff = currentIndex - selectedIndex;
                int absDiff = Math.Abs(diff);

                if (diff == 0)
                {
                    // The Center (Selected) Item
                    scaleX = scaleY = 1.4 * adaptiveBase;
                    translateY = -10 * adaptiveBase; // Pop it up slightly
                }
                else
                {
                    // Side Items
                    switch (CurrentMode)
                    {
                        case CarouselEffect.ModernStack:
                            ApplyModernStack(diff, absDiff, adaptiveBase, out scaleX, out scaleY, out translateX, out skewY);
                            break;

                        case CarouselEffect.ModernStackArc:
                            ApplyModernStack(diff, absDiff, adaptiveBase, out scaleX, out scaleY, out translateX, out skewY);
                            translateY = absDiff * 18.0 * adaptiveBase;
                            break;

                        case CarouselEffect.ConsoleShelf:
                            scaleX = scaleY = 0.85 * adaptiveBase;
                            // Pull items inward to create an overlapping "fan" look
                            translateX = (diff < 0 ? 40 : -40) * adaptiveBase;
                            break;

                        case CarouselEffect.DeepSpiral:
                            scaleX = scaleY = Math.Max(0.3, 1.0 - (absDiff * 0.2)) * adaptiveBase;
                            translateX = (diff * 20 * adaptiveBase);
                            translateY = (absDiff * 30 * adaptiveBase);
                            skewY = diff * 10;
                            break;

                        case CarouselEffect.Wave:
                            scaleX = scaleY = 0.9 * adaptiveBase;
                            translateY = Math.Sin(absDiff * 0.8) * 50 * adaptiveBase;
                            break;

                        case CarouselEffect.CardsOnTable:
                            scaleX = 0.9 * adaptiveBase;
                            scaleY = 0.6 * adaptiveBase;
                            skewY = (diff < 0 ? -15 : 15);
                            translateY = 40 * adaptiveBase;
                            break;

                        case CarouselEffect.Skyline:
                            scaleX = scaleY = 0.9 * adaptiveBase;
                            translateY = (currentIndex % 2 == 0 ? 0 : 50) * adaptiveBase;
                            break;

                        case CarouselEffect.InvertedV:
                            scaleX = scaleY = 0.9 * adaptiveBase;
                            // Cleaned up translateX: now we just add a small "lean" offset
                            translateX = (diff < 0 ? 30 : -30) * adaptiveBase;
                            skewY = diff < 0 ? -8 : 8;
                            break;

                        case CarouselEffect.FlatZoom:
                            scaleX = scaleY = 0.9 * adaptiveBase;
                            break;
                    }
                }
            }
        }

        // Build the CSS-like transform string
        string transformString = string.Format(CultureInfo.InvariantCulture,
            "translate({0:F2}px, {1:F2}px) scale({2:F3}, {3:F3}) skewY({4:F2}deg)",
            translateX, translateY, scaleX, scaleY, skewY);

        try { return TransformOperations.Parse(transformString); }
        catch { return TransformOperations.Parse("scale(1,1)"); }
    }

    private void ApplyModernStack(int diff, int absDiff, double adaptiveBase,
        out double sX, out double sY, out double tX, out double skY)
    {
        double widthComp = Math.Max(0.4, 1.0 - (absDiff * 0.15));
        double heightRed = Math.Max(0.7, 1.0 - (absDiff * 0.05));
        sX = 0.9 * adaptiveBase * widthComp;
        sY = 0.9 * adaptiveBase * heightRed;

        // Items pull toward the center for a tight "stack"
        tX = (diff < 0 ? 50 : -50) * adaptiveBase;
        skY = diff < 0 ? -4 : 4;
    }
}