using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Transformation;
using MySteamLibrary.Models;

namespace MySteamLibrary.Converters;

public class CarouselTransformConverter : IMultiValueConverter
{
    // Made public and static so it can be accessed from CarouselView
    public static CarouselEffect CurrentMode { get; set; } = CarouselEffect.ModernStack;

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        double windowWidth = 1920.0;
        double windowHeight = 1080.0;

        if (values.Count > 3)
        {
            if (values[3] is Rect rect)
            {
                windowWidth = rect.Width;
                windowHeight = rect.Height;
            }
            else if (values[3] is double d)
            {
                windowWidth = d;
            }
        }

        double adaptiveBase = Math.Clamp(windowWidth / 1920.0, 0.75, 1.1);


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
                // Enhanced adaptive zoom based on window size
                scaleX = scaleY = CalculateAdaptiveZoom(windowWidth, windowHeight, adaptiveBase);

                if (CurrentMode == CarouselEffect.ModernStackArc || CurrentMode == CarouselEffect.Wave)
                {
                    translateY = -20 * adaptiveBase;
                }
                else
                {
                    translateY = 0;
                }
            }
            else if (selectedIndex != -1)
            {
                switch (CurrentMode)
                {
                    case CarouselEffect.ModernStack:
                        ApplyModernStack(diff, absDiff, adaptiveBase, out scaleX, out scaleY, out translateX, out skewY);
                        translateY = 0;
                        break;

                    case CarouselEffect.ModernStackArc:
                        scaleX = scaleY = Math.Max(0.5, 1.0 - (absDiff * 0.12)) * adaptiveBase;
                        translateX = (diff * 50) * adaptiveBase;
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
                    // ===== NEW EFFECTS =====

                    case CarouselEffect.Tornado:
                        // Spiral rotation with vertical displacement
                        scaleX = scaleY = Math.Max(0.4, 1.0 - (absDiff * 0.15)) * adaptiveBase;
                        translateX = (diff * 30 * adaptiveBase);
                        translateY = (absDiff * 15 * adaptiveBase) * (diff < 0 ? 1 : -1);
                        skewY = diff * 20;
                        break;

                    case CarouselEffect.Waterfall:
                        // Cascading diagonal flow
                        scaleX = scaleY = Math.Max(0.5, 1.0 - (absDiff * 0.1)) * adaptiveBase;
                        translateX = (diff * 40 * adaptiveBase);
                        translateY = (absDiff * 30 * adaptiveBase);
                        skewY = diff < 0 ? -10 : 10;
                        break;

                    case CarouselEffect.Rollercoaster:
                        // Up and down arc motion  
                        scaleX = scaleY = 0.85 * adaptiveBase;
                        translateX = (diff * 20 * adaptiveBase);
                        translateY = -(diff * diff * 8 * adaptiveBase) + 30 * adaptiveBase;
                        break;

                    case CarouselEffect.FanSpread:
                        // Cards spread like a hand of cards
                        scaleX = scaleY = Math.Max(0.6, 1.0 - (absDiff * 0.08)) * adaptiveBase;
                        translateX = (diff * 60 * adaptiveBase);
                        translateY = (absDiff * absDiff * 3 * adaptiveBase);
                        skewY = diff * -12;
                        break;

                    case CarouselEffect.Accordion:
                        // Compressed horizontal squeeze
                        scaleX = Math.Max(0.3, 1.0 - (absDiff * 0.2)) * adaptiveBase;
                        scaleY = 0.9 * adaptiveBase;
                        translateX = (diff * 15 * adaptiveBase);
                        translateY = 0;
                        break;

                    case CarouselEffect.Pendulum:
                        // Swinging motion side to side
                        scaleX = scaleY = 0.85 * adaptiveBase;
                        translateX = (diff * 25 * adaptiveBase);
                        translateY = Math.Abs(Math.Sin(diff * 0.5)) * 40 * adaptiveBase;
                        skewY = diff * 5;
                        break;

                    case CarouselEffect.Staircase:
                        // Diagonal ascending/descending steps
                        scaleX = scaleY = Math.Max(0.5, 1.0 - (absDiff * 0.12)) * adaptiveBase;
                        translateX = (diff * 35 * adaptiveBase);
                        translateY = -(diff * 25 * adaptiveBase);
                        break;

                    case CarouselEffect.Helix:
                        // 3D DNA-like double spiral
                        scaleX = scaleY = Math.Max(0.4, 1.0 - (absDiff * 0.15)) * adaptiveBase;
                        translateX = (Math.Cos(diff * 0.5) * 50 * adaptiveBase);
                        translateY = (diff * 15 * adaptiveBase);
                        skewY = Math.Sin(diff * 0.5) * 15;
                        break;

                    case CarouselEffect.Ripple:
                        // Concentric circular wave from center
                        scaleX = scaleY = (0.8 + Math.Abs(Math.Sin(absDiff * 0.6)) * 0.3) * adaptiveBase;
                        translateX = (diff * 20 * adaptiveBase);
                        translateY = Math.Sin(absDiff * 0.6) * 30 * adaptiveBase;
                        break;

                    case CarouselEffect.Bounce:
                        // Alternating bounce heights
                        scaleX = scaleY = 0.85 * adaptiveBase;
                        translateX = (diff * 18 * adaptiveBase);
                        translateY = ((absDiff % 2 == 0) ? -25 : 15) * adaptiveBase;
                        break;

                    case CarouselEffect.Cylinder:
                        // Wrapped around imaginary cylinder
                        scaleX = scaleY = Math.Max(0.5, Math.Cos(diff * 0.3) * 0.5 + 0.5) * adaptiveBase;
                        translateX = (diff * 35 * adaptiveBase);
                        translateY = Math.Sin(diff * 0.3) * 60 * adaptiveBase;
                        skewY = diff * -8;
                        break;
                    case CarouselEffect.Perspective3D:
                        // Aggressive 3D perspective tilt
                        scaleX = scaleY = Math.Max(0.3, 1.0 - (absDiff * 0.18)) * adaptiveBase;
                        translateX = (diff * 70 * adaptiveBase);
                        translateY = (diff * 30 * adaptiveBase);
                        skewY = diff * 25;
                        break;

                    case CarouselEffect.Zipper:
                        // Interlocking zigzag pattern
                        scaleX = scaleY = 0.85 * adaptiveBase;
                        translateX = (diff * 25 * adaptiveBase);
                        translateY = ((diff % 3 == 0) ? -30 : (diff % 3 == 1) ? 0 : 30) * adaptiveBase;
                        skewY = (diff % 2 == 0) ? 8 : -8;
                        break;

                    case CarouselEffect.Domino:
                        // Progressive falling domino effect
                        scaleX = scaleY = Math.Max(0.5, 1.0 - (absDiff * 0.1)) * adaptiveBase;
                        translateX = (diff * 30 * adaptiveBase);
                        translateY = (absDiff * 12 * adaptiveBase);
                        skewY = diff * 18;
                        break;

                    case CarouselEffect.Rainbow:
                        // Smooth parabolic arc
                        scaleX = scaleY = Math.Max(0.6, 1.0 - (absDiff * 0.08)) * adaptiveBase;
                        translateX = (diff * 35 * adaptiveBase);
                        // Inverted parabola: y = -(x²) to create rainbow arc
                        translateY = -(Math.Pow(diff, 2) * 5 * adaptiveBase) + (absDiff * 15 * adaptiveBase);
                        break;

                    case CarouselEffect.Telescope:
                        // Expanding scale from center
                        scaleX = scaleY = (0.5 + (absDiff * 0.15)) * adaptiveBase;
                        translateX = (diff * 45 * adaptiveBase);
                        translateY = 0;
                        break;

                    case CarouselEffect.Flip:
                        // Cards flipping/rotating
                        scaleX = Math.Max(0.3, Math.Abs(Math.Cos(diff * 0.4))) * adaptiveBase;
                        scaleY = 0.9 * adaptiveBase;
                        translateX = (diff * 40 * adaptiveBase);
                        translateY = Math.Sin(diff * 0.4) * 20 * adaptiveBase;
                        skewY = Math.Sin(diff * 0.4) * 20;
                        break;

                    case CarouselEffect.Orbit:
                        // Circular orbital motion
                        double angle = diff * 0.4;
                        scaleX = scaleY = Math.Max(0.5, 1.0 - (absDiff * 0.12)) * adaptiveBase;
                        translateX = (Math.Cos(angle) * 60 + diff * 25) * adaptiveBase;
                        translateY = (Math.Sin(angle) * 60) * adaptiveBase;
                        skewY = diff * 6;
                        break;

                    case CarouselEffect.Pyramid:
                        // Triangular stacking formation
                        scaleX = scaleY = Math.Max(0.4, 1.0 - (absDiff * 0.15)) * adaptiveBase;
                        translateX = (diff * 35 * adaptiveBase);
                        // Triangle shape: cards stack higher the further from center
                        translateY = -(absDiff * 20 * adaptiveBase);
                        skewY = diff * -10;
                        break;

                    case CarouselEffect.Drift:
                        // Floating drift with pseudo-random offsets
                        scaleX = scaleY = (0.75 + (Math.Abs(Math.Sin(diff * 1.7)) * 0.2)) * adaptiveBase;
                        translateX = (diff * 30 * adaptiveBase);
                        // Pseudo-random using multiple sine waves
                        translateY = (Math.Sin(diff * 1.3) * 25 + Math.Sin(diff * 2.7) * 15) * adaptiveBase;
                        skewY = Math.Sin(diff * 1.1) * 10;
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

    /// <summary>
    /// Calculates enhanced adaptive zoom for selected card based on window dimensions.
    /// Provides progressive zoom: bigger windows get dramatically larger zoom.
    /// </summary>
    private double CalculateAdaptiveZoom(double windowWidth, double windowHeight, double adaptiveBase)
    {
        // Calculate adaptive base from both dimensions
        double widthFactor = Math.Clamp(windowWidth / 1920.0, 0.75, 1.5);
        double heightFactor = Math.Clamp(windowHeight / 1080.0, 0.8, 1.8);
        double combinedBase = (widthFactor + heightFactor) / 2.0;

        // Progressive zoom multiplier based on window height
        double zoomMultiplier;

        if (windowHeight < 700)
            zoomMultiplier = 1.3;      // Small windows: subtle zoom
        else if (windowHeight < 900)
            zoomMultiplier = 1.5;      // Medium windows: moderate zoom
        else if (windowHeight < 1200)
            zoomMultiplier = 1.7;      // Large windows: strong zoom
        else
            zoomMultiplier = 2.0;      // XL windows: dramatic zoom

        return zoomMultiplier * combinedBase;
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