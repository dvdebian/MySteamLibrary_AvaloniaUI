using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MySteamLibrary.ViewModels;

/// <summary>
/// Manages the configuration data for the application.
/// These properties are bound to the TextBoxes in SettingsView.axaml.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly string _settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private readonly string _cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");

    // Reference to CacheService for cache management operations
    private Services.CacheService? _cacheService;

    // Reference to MainViewModel to clear data from memory
    private MainViewModel? _mainViewModel;

    // A delegate (callback) that the MainViewModel will provide.
    // This allows this ViewModel to request a close without knowing about MainViewModel.
    public Action? RequestClose { get; set; }

    [ObservableProperty]
    private string _cacheInfo = "Loading cache info...";

    // The Steam Web API Key required to fetch user data.
    [ObservableProperty]
    private string _steamApiKey = string.Empty;

    // The unique 64-bit Steam ID for the user.
    [ObservableProperty]
    private string _steamId = string.Empty;

    // The local path where Steam is installed.
    [ObservableProperty]
    private string _steamPath = @"C:\Program Files (x86)\Steam";

    // The selected carousel effect mode
    [ObservableProperty]
    private Models.CarouselEffect _selectedCarouselEffect = Models.CarouselEffect.ModernStack;

    public SettingsViewModel()
    {
        // Load settings from disk on startup
        _ = LoadSettingsAsync();

        // Initialize cache service and update info
        UpdateCacheInfo();
    }

    /// <summary>
    /// Sets the cache service reference and updates cache info
    /// </summary>
    public void SetCacheService(Services.CacheService cacheService)
    {
        _cacheService = cacheService;
        UpdateCacheInfo();
    }

    /// <summary>
    /// Sets the MainViewModel reference for clearing data
    /// </summary>
    public void SetMainViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    /// <summary>
    /// Updates the cache information display
    /// </summary>
    private void UpdateCacheInfo()
    {
        if (_cacheService == null)
        {
            CacheInfo = "Cache service not initialized";
            return;
        }

        try
        {
            var (imageCount, totalSize) = _cacheService.GetCacheStats();
            double sizeMB = totalSize / (1024.0 * 1024.0);

            string cacheFolder = _cacheService.GetCacheFolder();
            CacheInfo = $"Location: {cacheFolder}\n" +
                       $"Images: {imageCount}\n" +
                       $"Total Size: {sizeMB:F2} MB";
        }
        catch
        {
            CacheInfo = "Unable to get cache information";
        }
    }

    /// <summary>
    /// Loads the API Key and Steam ID from a local JSON file.
    /// </summary>
    private async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsFile))
            {
                var json = await File.ReadAllTextAsync(_settingsFile);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);

                if (settings != null)
                {
                    SteamApiKey = settings.SteamApiKey ?? string.Empty;
                    SteamId = settings.SteamId ?? string.Empty;
                    SteamPath = settings.SteamPath ?? @"C:\Program Files (x86)\Steam";
                    SelectedCarouselEffect = settings.SelectedCarouselEffect;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the current API Key and Steam ID to a local JSON file.
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new SettingsData
            {
                SteamApiKey = SteamApiKey,
                SteamId = SteamId,
                SteamPath = SteamPath,
                SelectedCarouselEffect = SelectedCarouselEffect
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Triggered by the Close button in the UI.
    /// Saves settings before closing.
    /// </summary>
    [RelayCommand]
    private async Task Close()
    {
        await SaveSettingsAsync();
        RequestClose?.Invoke();
    }


    /// <summary>
    /// Triggered by the Cancel button in the UI.
    /// Closes the settings window
    /// </summary>
    [RelayCommand]
    private async Task Cancel()
    {
        RequestClose?.Invoke();
    }

    /// <summary>
    /// Opens the cache folder in Windows Explorer
    /// </summary>
    [RelayCommand]
    private void OpenCacheFolder()
    {
        _cacheService?.OpenCacheFolderInExplorer();
    }

    /// <summary>
    /// Clears all user data: settings, cache, and images.
    /// </summary>
    [RelayCommand]
    private async Task ClearData()
    {
        try
        {
            // Clear the text boxes
            // SteamApiKey = string.Empty;
            // SteamId = string.Empty;

            // Delete the settings file
            if (File.Exists(_settingsFile))
            {
                File.Delete(_settingsFile);
            }

            // Delete the entire cache folder (includes library_cache.json and all images)
            string cacheFolder = _cacheService?.GetCacheFolder() ?? _cacheFolder;
            if (Directory.Exists(cacheFolder))
            {
                Directory.Delete(cacheFolder, recursive: true);
            }

            // Clear all game data from MainViewModel
            _mainViewModel?.ClearAllData();

            System.Diagnostics.Debug.WriteLine("✅ All data cleared successfully.");
            UpdateCacheInfo();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error clearing data: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Returns the current API Key. Used by SteamApiService.
    /// </summary>
    public string GetApiKey() => SteamApiKey;

    /// <summary>
    /// Returns the current Steam ID. Used by SteamApiService.
    /// </summary>
    public string GetSteamId() => SteamId;

    /// <summary>
    /// Helper class for JSON serialization of settings.
    /// </summary>
    private class SettingsData
    {
        public string? SteamApiKey { get; set; }
        public string? SteamId { get; set; }
        public string? SteamPath { get; set; }
        public Models.CarouselEffect SelectedCarouselEffect { get; set; } = Models.CarouselEffect.ModernStack;
    }
}