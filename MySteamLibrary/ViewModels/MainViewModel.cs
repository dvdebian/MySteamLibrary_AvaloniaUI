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

public enum SortCriteria
{
    Alphabetical,
    PlayTime,
    AppId
}

public partial class MainViewModel : ViewModelBase
{
    private readonly SteamApiService _steamService = new();
    private readonly CacheService _cacheService = new();

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

    [ObservableProperty]
    private SortCriteria _currentSortMode = SortCriteria.Alphabetical;

    public SettingsViewModel Settings { get; } = new();

    // The collection used by all views for display
    private readonly ObservableCollection<GameModel> _allGames = new();

    // The hidden master list that stores everything in memory for instant searching
    private readonly List<GameModel> _masterLibrary = new();

    public MainViewModel()
    {
        _listView = new ListViewModel { Games = _allGames };
        _gridView = new GridViewModel { Games = _allGames };
        _coverView = new CoverViewModel { Games = _allGames };
        _carouselView = new CarouselViewModel { Games = _allGames };

        _activeView = _listView;

        Settings.RequestClose = () => IsSettingsOpen = false;

        _ = InitializeLibraryAsync();
    }

    /// <summary>
    /// Triggered automatically by CommunityToolkit when SearchText changes.
    /// This ensures live filtering as the user types.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilteringAndSorting();
    }

    private async Task InitializeLibraryAsync()
    {
        try
        {
            var cachedGames = await _cacheService.LoadLibraryCacheAsync();

            if (cachedGames != null && cachedGames.Any())
            {
                // Load into memory first
                _masterLibrary.Clear();
                _masterLibrary.AddRange(cachedGames);

                // Show in UI
                ApplyFilteringAndSorting();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes the master library through search and sort filters, then updates the UI collection.
    /// Done entirely in memory for zero lag.
    /// </summary>
    private void ApplyFilteringAndSorting()
    {
        // 1. Filter based on search text
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _masterLibrary
            : _masterLibrary.Where(g => g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // 2. Sort based on criteria
        var sorted = CurrentSortMode switch
        {
            SortCriteria.PlayTime => filtered.OrderByDescending(g => g.PlaytimeMinutes),
            SortCriteria.AppId => filtered.OrderBy(g => g.AppId),
            _ => filtered.OrderBy(g => g.Title)
        };

        // 3. Update the display collection
        _allGames.Clear();
        foreach (var game in sorted)
        {
            _allGames.Add(game);
        }
    }

    [RelayCommand]
    public async Task RefreshLibrary()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            var freshGames = await _steamService.GetLibrarySkeletonAsync();

            if (freshGames != null && freshGames.Any())
            {
                // Update master memory
                _masterLibrary.Clear();
                _masterLibrary.AddRange(freshGames);

                // Update UI
                ApplyFilteringAndSorting();

                await _cacheService.SaveLibraryCacheAsync(_masterLibrary);
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

    private async Task BackgroundUpdateDataAsync()
    {
        // Update images and descriptions directly on the master objects
        var imageTasks = _masterLibrary.Select(game => _steamService.LoadGameImageAsync(game));
        await Task.WhenAll(imageTasks);

        await _cacheService.SaveLibraryCacheAsync(_masterLibrary);
        await _steamService.RefreshDescriptionsAsync(_masterLibrary);
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
    public void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    [RelayCommand]
    public void OpenGameDetails(GameModel game)
    {
        var detailsVm = new GameDetailsViewModel(game) { RequestClose = () => IsGameDetailsOpen = false };
        CurrentDetails = detailsVm;
        IsGameDetailsOpen = true;
    }

    [RelayCommand]
    public void CloseGameDetails()
    {
        IsGameDetailsOpen = false;
        CurrentDetails = null;
    }

    [RelayCommand]
    public void ChangeSortMode(SortCriteria newCriteria)
    {
        CurrentSortMode = newCriteria;
        ApplyFilteringAndSorting();
    }
}