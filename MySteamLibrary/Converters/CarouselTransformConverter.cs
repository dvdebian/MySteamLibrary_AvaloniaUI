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
    ModernStack,      // Your current favorite
    InvertedV,        // Classic lean
    ModernStackArc,   // The "dipping" look
    FlatZoom,         // No skew, just zoom
    // --- NEW MODES ---
    ConsoleShelf,     // Flat, tight overlap, no height change (like PS5/Switch)
    DeepSpiral,       // Aggressive scaling and rotation for a 3D tunnel look
    Wave,             // Up and down oscillation
    CardsOnTable,     // Perspective that makes cards look like they are lying back
    Skyline           // Alternating heights for a jagged, urban look
}

public class CarouselTransformConverter : IMultiValueConverter
{
    // CHANGE THIS TO TEST DIFFERENT LOOKS
    private const CarouselEffect CurrentMode = CarouselEffect.Wave;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        double windowWidth = 1920.0;
        if (values.Count > 3)
        {
            if (values[3] is Rect rect) windowWidth = rect.Width;
            else if (values[3] is double d) windowWidth = d;
        }
        double adaptiveBase = Math.Clamp(windowWidth / 1920.0, 0.75, 1.1);

        // --- HEIGHT FIX ---
        if (parameter?.ToString() == "GetHeight")
        {
            // Arc and Wave need more space because they "dip". 
            // Flat modes (ModernStack) use 1.4 to stay closer to the title.
            bool needsExtraHeight = CurrentMode == CarouselEffect.ModernStackArc || CurrentMode == CarouselEffect.Wave;
            return 330 * (needsExtraHeight ? 1.6 : 1.6) * adaptiveBase;
        }

        double scaleX = 1.0, scaleY = 1.0, skewY = 0.0, translateX = 0.0, translateY = 0.0;

        if (values.Count >= 3 && values[0] is GameModel currentItem && values[2] is IEnumerable collection)
        {
            var selectedItem = values[1] as GameModel;
            var list = new List<object>();
            foreach (var item in collection) if (item != null) list.Add(item);

            int currentIndex = list.IndexOf(currentItem);
            int selectedIndex = selectedItem != null ? list.IndexOf(selectedItem) : -1;
            int diff = currentIndex - selectedIndex;
            int absDiff = Math.Abs(diff);

            if (diff == 0)
            {
                // CENTER ITEM LOGIC
                scaleX = scaleY = 1.4 * adaptiveBase;

                // Lift the center item ONLY for modes that dip down.
                // This keeps the center of ModernStack perfectly centered in its row.
                if (CurrentMode == CarouselEffect.ModernStackArc || CurrentMode == CarouselEffect.Wave)
                {
                    translateY = -20 * adaptiveBase;
                }
                else
                {
                    translateY = 0; // Flat modes stay at the baseline
                }
            }
            else if (selectedIndex != -1)
            {
                switch (CurrentMode)
                {
                    case CarouselEffect.ModernStack:
                        ApplyModernStack(diff, absDiff, adaptiveBase, out scaleX, out scaleY, out translateX, out skewY);
                        // Ensure ModernStack stays on the baseline
                        translateY = 0;
                        break;

                    case CarouselEffect.ModernStackArc:
                        scaleX = scaleY = Math.Max(0.5, 1.0 - (absDiff * 0.12)) * adaptiveBase;
                        translateX = (diff * 50) * adaptiveBase;

                        // --- ARC ADJUSTMENT ---
                        // We use a small lift (-20) so the center is higher, 
                        // but the dipping sides don't go past the "floor" of the row.
                        double dropPerItem = 20 * adaptiveBase;
                        double liftOffset = 20 * adaptiveBase;
                        translateY = (absDiff * dropPerItem) - liftOffset;

                        skewY = (diff * 4);
                        break;

                    case CarouselEffect.ConsoleShelf:
                        scaleX = scaleY = 0.85 * adaptiveBase;
                        translateX = (diff < 0 ? 60 : -60) * adaptiveBase;
                        translateY = 0;
                        break;

                    case CarouselEffect.DeepSpiral:
                        scaleX = scaleY = Math.Max(0.3, 1.0 - (absDiff * 0.2)) * adaptiveBase;
                        translateX = (diff * 40 * adaptiveBase);
                        translateY = (diff * 20 * adaptiveBase);
                        skewY = diff * 15;
                        break;

                    case CarouselEffect.Wave:
                        scaleX = scaleY = 0.9 * adaptiveBase;
                        translateX = (diff * 15 * adaptiveBase);
                        // Center wave around a slightly lifted baseline
                        translateY = (Math.Sin(diff * 0.8) * 35 * adaptiveBase) - (30 * adaptiveBase);
                        break;

                    case CarouselEffect.CardsOnTable:
                        scaleX = 0.9 * adaptiveBase;
                        scaleY = 0.6 * adaptiveBase;
                        skewY = (diff < 0 ? -15 : 15);
                        translateX = (diff * 20 * adaptiveBase);
                        translateY = 20 * adaptiveBase;
                        break;

                    case CarouselEffect.Skyline:
                        scaleX = scaleY = 0.9 * adaptiveBase;
                        translateY = (currentIndex % 2 == 0 ? -20 : 20) * adaptiveBase;
                        translateX = (diff * 5 * adaptiveBase);
                        break;

                    case CarouselEffect.InvertedV:
                        scaleX = scaleY = 0.9 * adaptiveBase;
                        translateX = (diff < 0 ? 80 : -80) * adaptiveBase + (diff * -15 * adaptiveBase);
                        skewY = diff < 0 ? -8 : 8;
                        translateY = 0;
                        break;

                    case CarouselEffect.FlatZoom:
                        scaleX = scaleY = 0.9 * adaptiveBase;
                        translateX = (diff * 10 * adaptiveBase);
                        translateY = 0;
                        break;
                }
            }
        }

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
        double pull = 105.0 * adaptiveBase;
        tX = (diff < 0 ? pull : -pull) + (diff * -12 * adaptiveBase);
        skY = diff < 0 ? -4 : 4;
    }
}