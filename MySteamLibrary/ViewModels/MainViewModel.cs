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

/// <summary>
/// Defines the available sorting methods for the library.
/// </summary>
public enum SortCriteria
{
    Alphabetical,
    PlayTime,
    AppId
}

/// <summary>
/// The primary controller for the application.
/// Manages navigation, data orchestration, and global state like selection and searching.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    // Business logic services
    private readonly SteamApiService _steamService = new();
    private readonly CacheService _cacheService = new();

    // View instances held in memory to preserve state (like scroll position) when switching views
    private readonly ListViewModel _listView;
    private readonly GridViewModel _gridView;
    private readonly CoverViewModel _coverView;
    private readonly CarouselViewModel _carouselView;

    // The current view being displayed in the MainView's ContentControl
    [ObservableProperty]
    private LibraryPresenterViewModel _activeView;

    // UI State flags for overlays and loading indicators
    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isGameDetailsOpen;

    [ObservableProperty]
    private bool _isRefreshing;

    // Holds the ViewModel for the currently inspected game
    [ObservableProperty]
    private GameDetailsViewModel? _currentDetails;

    // Bound to the search bar; triggers OnSearchTextChanged when modified
    [ObservableProperty]
    private string _searchText = string.Empty;

    // The user's preferred sorting method
    [ObservableProperty]
    private SortCriteria _currentSortMode = SortCriteria.Alphabetical;

    /// <summary>
    /// Configuration and user credentials logic.
    /// Declared as a property so it is accessible throughout the class and to the UI.
    /// </summary>
    public SettingsViewModel Settings { get; } = new();

    /// <summary>
    /// GLOBAL SELECTION: This is the single source of truth for the "focused" game.
    /// Centered views (Cover/Carousel) bind to this to know which item to move to the middle.
    /// </summary>
    [ObservableProperty]
    private GameModel? _selectedGame;

    // The collection bound to all UI views (List, Grid, etc.)
    private readonly ObservableCollection<GameModel> _allGames = new();

    // The hidden 'Source of Truth' containing every game, used for high-speed in-memory filtering
    private readonly List<GameModel> _masterLibrary = new();

    public MainViewModel()
    {
        // Initialize sub-viewmodels and pass 'this' as the Parent.
        // This allows sub-views to access Parent.SelectedGame for global syncing.
        _listView = new ListViewModel { Games = _allGames, Parent = this };
        _gridView = new GridViewModel { Games = _allGames, Parent = this };
        _coverView = new CoverViewModel { Games = _allGames, Parent = this };
        _carouselView = new CarouselViewModel { Games = _allGames, Parent = this };

        // Start the application in List view by default
        _activeView = _listView;

        // Callback to close the settings overlay from within the SettingsViewModel
        Settings.RequestClose = () => IsSettingsOpen = false;

        // Load data from local disk immediately on startup
        _ = InitializeLibraryAsync();
    }

    /// <summary>
    /// Automatically called by the CommunityToolkit whenever the SearchText property changes.
    /// Triggers the filtering logic to update the UI in real-time.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilteringAndSorting();
    }

    /// <summary>
    /// Loads the library from the local JSON cache to provide an instant startup experience.
    /// </summary>
    private async Task InitializeLibraryAsync()
    {
        try
        {
            var cachedGames = await _cacheService.LoadLibraryCacheAsync();

            if (cachedGames != null && cachedGames.Any())
            {
                _masterLibrary.Clear();
                _masterLibrary.AddRange(cachedGames);

                // Refresh the visible collection based on current sort/filter
                ApplyFilteringAndSorting();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load cache: {ex.Message}");
        }
    }

    /// <summary>
    /// The 'Processor' method. Takes the master list, applies the search filter,
    /// applies the sort order, and updates the observable collection for the UI.
    /// </summary>
    private void ApplyFilteringAndSorting()
    {
        // Step 1: Filter by Title (Case-insensitive)
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _masterLibrary
            : _masterLibrary.Where(g => g.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // Step 2: Order the filtered results
        var sorted = CurrentSortMode switch
        {
            SortCriteria.PlayTime => filtered.OrderByDescending(g => g.PlaytimeMinutes),
            SortCriteria.AppId => filtered.OrderBy(g => g.AppId),
            _ => filtered.OrderBy(g => g.Title)
        };

        // Step 3: Update UI collection (Clear/Add is used to keep the same instance bound to views)
        _allGames.Clear();
        foreach (var game in sorted)
        {
            _allGames.Add(game);
        }
    }

    /// <summary>
    /// Fetches fresh data from Steam API. 
    /// Updates the Master list first, then triggers the UI update.
    /// </summary>
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
                _masterLibrary.Clear();
                _masterLibrary.AddRange(freshGames);
                ApplyFilteringAndSorting();

                // Save to disk and start fetching high-res images/descriptions in the background
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

    /// <summary>
    /// Handles the secondary and tertiary stages of loading (Images and Descriptions).
    /// Updates the objects in the master library directly.
    /// </summary>
    private async Task BackgroundUpdateDataAsync()
    {
        var imageTasks = _masterLibrary.Select(game => _steamService.LoadGameImageAsync(game));
        await Task.WhenAll(imageTasks);

        await _cacheService.SaveLibraryCacheAsync(_masterLibrary);
        await _steamService.RefreshDescriptionsAsync(_masterLibrary);
    }

    /// <summary>
    /// Switches the active view mode and ensures a selection exists for centering-based views.
    /// </summary>
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

        // If switching to a view that requires a centered anchor, auto-select the first game if none selected.
        if (mode == "Cover" || mode == "Carousel")
        {
            if (SelectedGame == null && _allGames.Any())
            {
                SelectedGame = _allGames[0];
            }
        }
    }

    [RelayCommand]
    public void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;

    /// <summary>
    /// Navigates to the details view for a specific game.
    /// </summary>
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

    /// <summary>
    /// Updates the sort criteria and refreshes the displayed list.
    /// </summary>
    [RelayCommand]
    public void ChangeSortMode(SortCriteria newCriteria)
    {
        CurrentSortMode = newCriteria;
        ApplyFilteringAndSorting();
    }
}