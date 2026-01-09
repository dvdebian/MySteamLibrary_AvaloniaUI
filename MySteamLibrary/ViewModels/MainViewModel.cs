using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySteamLibrary.Models;
using MySteamLibrary.Services;
using MySteamLibrary.ViewModels;

namespace MySteamLibrary.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // Services for API data and local persistence
    private readonly SteamApiService _steamService = new();
    private readonly CacheService _cacheService = new();

    // Fields to store the pre-created view models for navigation
    private readonly ListViewModel _listView;
    private readonly GridViewModel _gridView;
    private readonly CoverViewModel _coverView;
    private readonly CarouselViewModel _carouselView;

    [ObservableProperty]
    private LibraryPresenterViewModel _activeView;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isGameDetailsOpen;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private GameDetailsViewModel? _currentDetails;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SettingsViewModel Settings { get; } = new();

    // The master collection that all sub-views bind to
    private readonly ObservableCollection<GameModel> _allGames = new();

    public MainViewModel()
    {
        // Initialize sub-view models with the shared game collection
        _listView = new ListViewModel { Games = _allGames };
        _gridView = new GridViewModel { Games = _allGames };
        _coverView = new CoverViewModel { Games = _allGames };
        _carouselView = new CarouselViewModel { Games = _allGames };

        // Default view on startup
        _activeView = _listView;

        // Hook up settings close action
        Settings.RequestClose = () => IsSettingsOpen = false;

        // Startup: Load only from cache. No automatic network fetch.
        _ = InitializeLibraryAsync();
    }

    /// <summary>
    /// Initial load that only looks at the local cache.
    /// </summary>
    private async Task InitializeLibraryAsync()
    {
        try
        {
            var cachedGames = await _cacheService.LoadLibraryCacheAsync();

            if (cachedGames != null && cachedGames.Any())
            {
                _allGames.Clear();
                foreach (var game in cachedGames)
                {
                    _allGames.Add(game);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Stage-based Refresh: 1. Skeleton UI, 2. Background Images, 3. Background Descriptions.
    /// </summary>
    [RelayCommand]
    public async Task RefreshLibrary()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;

            // STAGE 1: Fast Skeleton Load (Titles and Playtime only)
            var freshGames = await _steamService.GetLibrarySkeletonAsync();

            if (freshGames != null && freshGames.Any())
            {
                _allGames.Clear();
                foreach (var game in freshGames)
                {
                    _allGames.Add(game);
                }

                // Initial cache save with skeletons
                await _cacheService.SaveLibraryCacheAsync(_allGames.ToList());

                // STAGE 2 & 3: Launch background updates without 'awaiting' them here
                // This allows IsRefreshing to finish quickly so the UI isn't blocked.
                _ = Task.Run(() => BackgroundUpdateDataAsync());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during refresh: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Background worker to fill in images and descriptions piece-by-piece.
    /// </summary>
    private async Task BackgroundUpdateDataAsync()
    {
        var gameList = _allGames.ToList();

        // 1. STAGE 2: Fast Parallel Image Download
        // We run these in parallel because Steam's CDN is fast and not rate-limited like the Store API.
        var imageTasks = gameList.Select(game => _steamService.LoadGameImageAsync(game));
        await Task.WhenAll(imageTasks);

        // Save cache once images are linked
        await _cacheService.SaveLibraryCacheAsync(_allGames.ToList());

        // 2. STAGE 3: Sequential Description Fetch (Rate-limited)
        // This method handles its own internal 1.5s delay and periodic cache saving.
        await _steamService.RefreshDescriptionsAsync(_allGames);
    }

    [RelayCommand]
    public void SelectMode(string mode)
    {
        ActiveView = mode switch
        {
            "Grid" => _gridView,
            "Cover" => _coverView,
            "Carousel" => _carouselView,
            _ => _listView
        };
    }

    [RelayCommand]
    public void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    public void OpenGameDetails(GameModel game)
    {
        var detailsVm = new GameDetailsViewModel(game);
        detailsVm.RequestClose = () => IsGameDetailsOpen = false;
        CurrentDetails = detailsVm;
        IsGameDetailsOpen = true;
    }

    [RelayCommand]
    public void CloseGameDetails()
    {
        IsGameDetailsOpen = false;
        CurrentDetails = null;
    }
}