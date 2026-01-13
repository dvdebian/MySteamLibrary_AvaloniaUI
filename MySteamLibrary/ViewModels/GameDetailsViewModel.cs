using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySteamLibrary.Models;
using MySteamLibrary.Services;
using System;
using System.Threading.Tasks;

namespace MySteamLibrary.ViewModels;

public partial class GameDetailsViewModel : ViewModelBase
{
    private readonly CustomImageService? _customImageService;
    private readonly CacheService? _cacheService;

    // Callback for the parent to handle closing this view
    public Action? RequestClose { get; set; }

    // Callback to get the parent window (needed for file picker dialog)
    public Func<Avalonia.Controls.Window?>? GetParentWindow { get; set; }

    // Callback to save cache after image change
    public Func<Task>? OnImageChanged { get; set; }

    [ObservableProperty]
    private GameModel? _selectedGame;

    public GameDetailsViewModel(GameModel game, CacheService? cacheService = null)
    {
        SelectedGame = game;
        _cacheService = cacheService;

        // Initialize custom image service if cache service is provided
        if (_cacheService != null)
        {
            _customImageService = new CustomImageService(_cacheService);
        }
    }

    /// <summary>
    /// Executes when the user clicks the "BACK" or "X" button.
    /// Notifies the parent (MainViewModel) to hide this overlay.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    /// <summary>
    /// Opens a file picker to allow the user to select a custom cover image.
    /// </summary>
    [RelayCommand]
    private async Task SetCustomImage()
    {
        if (SelectedGame == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️  No game selected");
            return;
        }

        if (_customImageService == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️  CustomImageService not initialized");
            return;
        }

        // Get parent window for file picker
        var parentWindow = GetParentWindow?.Invoke();
        if (parentWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("⚠️  Cannot get parent window for file picker");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"🖼️  Opening file picker for: {SelectedGame.Title}");

        // Open file picker and save custom image
        bool success = await _customImageService.SelectAndSaveCustomImageAsync(SelectedGame, parentWindow);

        if (success)
        {
            System.Diagnostics.Debug.WriteLine($"✅ Custom image set successfully for {SelectedGame.Title}");

            // CRITICAL: Force image reload by temporarily clearing the path
            // This is necessary because Avalonia's Bitmap caches images by path
            // Even though the file content changed, the path is the same, so UI won't reload
            string newImagePath = SelectedGame.ImagePath;
            SelectedGame.ImagePath = string.Empty;  // Clear to force unload

            // Small delay to ensure UI processes the empty path
            await Task.Delay(10);

            // Set the path back to trigger reload with new image content
            SelectedGame.ImagePath = newImagePath;

            // Notify that the image changed so cache can be saved
            if (OnImageChanged != null)
            {
                await OnImageChanged.Invoke();
            }

            // Force UI update by notifying that SelectedGame changed
            OnPropertyChanged(nameof(SelectedGame));
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ℹ️  Custom image not set (user cancelled or error occurred)");
        }
    }

    /// <summary>
    /// Removes the cached image and attempts to re-download from Steam CDN.
    /// </summary>
    [RelayCommand]
    private async Task RemoveCustomImage()
    {
        if (SelectedGame == null || _customImageService == null || _cacheService == null)
            return;

        System.Diagnostics.Debug.WriteLine($"🔄 Refreshing image for: {SelectedGame.Title}");

        // Step 1: Remove the cached image file
        bool removed = await _customImageService.RemoveCustomImageAsync(SelectedGame);

        if (removed)
        {
            System.Diagnostics.Debug.WriteLine($"✅ Cached image removed for {SelectedGame.Title}");

            // Step 2: Attempt to re-download from Steam CDN
            System.Diagnostics.Debug.WriteLine($"⬇️  Attempting to re-download from Steam CDN...");

            string remoteUrl = $"https://cdn.akamai.steamstatic.com/steam/apps/{SelectedGame.AppId}/library_600x900_2x.jpg";
            string newImagePath = await _cacheService.GetOrDownloadImageAsync(SelectedGame.AppId, remoteUrl);

            // Update the game's image path
            SelectedGame.ImagePath = newImagePath;

            if (!string.IsNullOrEmpty(newImagePath))
            {
                System.Diagnostics.Debug.WriteLine($"✅ Image re-downloaded successfully from Steam CDN");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️  No image available on Steam CDN, showing placeholder");
            }

            // Step 3: Save updated cache
            if (OnImageChanged != null)
            {
                await OnImageChanged.Invoke();
            }

            // Force UI update
            OnPropertyChanged(nameof(SelectedGame));
        }
    }
}