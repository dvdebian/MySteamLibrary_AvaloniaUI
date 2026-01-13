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
    /// Removes the custom image and reverts to attempting Steam CDN download or placeholder.
    /// </summary>
    [RelayCommand]
    private async Task RemoveCustomImage()
    {
        if (SelectedGame == null || _customImageService == null)
            return;

        System.Diagnostics.Debug.WriteLine($"🗑️  Removing custom image for: {SelectedGame.Title}");

        bool success = await _customImageService.RemoveCustomImageAsync(SelectedGame);

        if (success)
        {
            System.Diagnostics.Debug.WriteLine($"✅ Custom image removed for {SelectedGame.Title}");

            // Notify that the image changed
            if (OnImageChanged != null)
            {
                await OnImageChanged.Invoke();
            }

            // Force UI update
            OnPropertyChanged(nameof(SelectedGame));
        }
    }

    /// <summary>
    /// Returns true if the current game has a custom image set (file exists in cache).
    /// Used to show/hide the "Remove Custom Image" button.
    /// </summary>
    public bool HasCustomImage
    {
        get
        {
            if (SelectedGame == null || string.IsNullOrEmpty(SelectedGame.ImagePath))
                return false;

            // Check if the image path points to an actual file in cache
            return System.IO.File.Exists(SelectedGame.ImagePath);
        }
    }
}