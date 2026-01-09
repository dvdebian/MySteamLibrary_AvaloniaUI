using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MySteamLibrary.Converters;

public class BitmapValueConverter : IValueConverter
{
    public static readonly BitmapValueConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 1. If path is null or empty, provide the loading placeholder immediately
        if (value is not string path || string.IsNullOrEmpty(path))
        {
            return GetPlaceholder();
        }

        try
        {
            // 2. Load the actual image file from the disk
            return new Bitmap(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading image from {path}: {ex.Message}");
            // 3. Fallback to placeholder if the file is missing or broken
            return GetPlaceholder();
        }
    }

    /// <summary>
    /// Loads the placeholder image from the application resources.
    /// </summary>
    private static Bitmap? GetPlaceholder()
    {
        try
        {
            // Note: Ensure your placeholder.jpg is in the Assets folder 
            // and its Build Action is set to 'AvaloniaResource'
            var uri = new Uri("avares://MySteamLibrary/Assets/placeholder.png");
            var asset = AssetLoader.Open(uri);
            return new Bitmap(asset);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load placeholder asset: {ex.Message}");
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}