using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
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
    private readonly SteamApiService _steamService;
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

    // Error message to display when operations fail or credentials are missing
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // The user's preferred sorting method
    [ObservableProperty]
    private SortCriteria _currentSortMode = SortCriteria.Alphabetical;


    /// <summary>
    /// Computed properties for sort mode button states
    /// </summary>
    public bool IsSortAlphabetical => CurrentSortMode == SortCriteria.Alphabetical;
    public bool IsSortPlayTime => CurrentSortMode == SortCriteria.PlayTime;
    public bool IsSortAppId => CurrentSortMode == SortCriteria.AppId;

    /// <summary>
    /// ComboBox selected index for sorting (0=Alphabetical, 1=PlayTime, 2=AppId)
    /// </summary>
    [ObservableProperty]
    private int _currentSortModeIndex = 0;

    /// <summary>
    /// Called when the sort dropdown selection changes
    /// </summary>
    partial void OnCurrentSortModeIndexChanged(int value)
    {
        CurrentSortMode = value switch
        {
            1 => SortCriteria.PlayTime,
            2 => SortCriteria.AppId,
            _ => SortCriteria.Alphabetical
        };
        this.ApplyFilteringAndSorting();
    }
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

    /// <summary>
    /// Returns true if the current active view is the Carousel view.
    /// Used to show/hide the Effect button in the UI.
    /// </summary>
    public bool IsCarouselMode => ActiveView == _carouselView;

    /// <summary>
    /// Mode tracking properties for button state visualization
    /// </summary>
    [ObservableProperty]
    private bool _isListMode = true; // Start in List mode by default

    [ObservableProperty]
    private bool _isGridMode;

    [ObservableProperty]
    private bool _isCoverMode;


    // Filter: Show only played games
    [ObservableProperty]
    private bool _showOnlyPlayedGames;

    // Sync Progress Tracking Properties
    [ObservableProperty]
    private bool _isSyncPanelVisible;

    [ObservableProperty]
    private int _gameListTotal;

    [ObservableProperty]
    private int _gameListCurrent;

    [ObservableProperty]
    private bool _gameListCompleted;

    [ObservableProperty]
    private int _imagesTotal;

    [ObservableProperty]
    private int _imagesCurrent;

    [ObservableProperty]
    private bool _imagesCompleted;

    [ObservableProperty]
    private int _descriptionsTotal;

    [ObservableProperty]
    private int _descriptionsCurrent;

    [ObservableProperty]
    private bool _descriptionsCompleted;
    // Filter/Sync area collapse/expand state (controls visibility of both filter row and sync progress)
    [ObservableProperty]
    private bool _isSyncPanelExpanded = false;
    /// <summary>
    /// Returns true when there are no games in the library.
    /// Used to show the "No data found" message.
    /// </summary>
    public bool HasNoData => _allGames.Count == 0;

    // The collection bound to all UI views (List, Grid, etc.)
    private readonly ObservableCollection<GameModel> _allGames = new();

    // The hidden 'Source of Truth' containing every game, used for high-speed in-memory filtering
    private readonly List<GameModel> _masterLibrary = new();

    public MainViewModel()
    {
        // Initialize SteamApiService with the Settings reference
        _steamService = new SteamApiService(Settings);

        // Pass the cache service to Settings so it can display cache info
        Settings.SetCacheService(_cacheService);

        // Pass this MainViewModel to Settings so it can clear data
        Settings.SetMainViewModel(this);

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

    // ... rest of the MainViewModel code remains the same ...

    /// <summary>
    /// Automatically called by the CommunityToolkit whenever the SearchText property changes.
    /// Triggers the filtering logic to update the UI in real-time.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilteringAndSorting();
    }

    /// <summary>
    /// Automatically called when ShowOnlyPlayedGames property changes.
    /// </summary>
    partial void OnShowOnlyPlayedGamesChanged(bool value)
    {
        ApplyFilteringAndSorting();
    }

    /// <summary>
    /// Called automatically when ActiveView changes to notify UI about IsCarouselMode.
    /// </summary>
    partial void OnActiveViewChanged(LibraryPresenterViewModel value)
    {
        OnPropertyChanged(nameof(IsCarouselMode));
    }

    /// <summary>
    /// Loads the library from the local JSON cache to provide an instant startup experience.
    /// If sync was incomplete, automatically resumes it with visible progress panel.
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


                // Check if previous sync was incomplete
                bool isFullySynced = await _cacheService.LoadSyncStateAsync();

                // Always set progress values (whether complete or incomplete)
                int gamesWithImages = _masterLibrary.Count(g => !string.IsNullOrEmpty(g.ImagePath) && File.Exists(g.ImagePath));
                int gamesWithDescriptions = _masterLibrary.Count(g =>
                    !string.IsNullOrWhiteSpace(g.Description) &&
                    g.Description != "Loading description..." &&
                    g.Description != "No description available.");

                // Set up sync panel progress
                GameListTotal = _masterLibrary.Count;
                GameListCurrent = _masterLibrary.Count;
                GameListCompleted = true;

                ImagesTotal = _masterLibrary.Count;
                ImagesCurrent = gamesWithImages;
                ImagesCompleted = (gamesWithImages == _masterLibrary.Count);

                DescriptionsTotal = _masterLibrary.Count;
                DescriptionsCurrent = gamesWithDescriptions;
                DescriptionsCompleted = (gamesWithDescriptions == _masterLibrary.Count);

                if (!isFullySynced && _masterLibrary.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("🔄 Previous sync incomplete - auto-resuming...");

                    // Show sync panel (user can expand manually with toggle button)
                    IsSyncPanelVisible = true;
                    await _cacheService.SaveSyncStateAsync(false); // Mark sync as incomplete

                    System.Diagnostics.Debug.WriteLine($"📊 Resume progress: Images {ImagesCurrent}/{ImagesTotal}, Descriptions {DescriptionsCurrent}/{DescriptionsTotal}");

                    // Resume background sync
                    _ = Task.Run(() => BackgroundUpdateDataAsync());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Sync complete: Images {ImagesCurrent}/{ImagesTotal}, Descriptions {DescriptionsCurrent}/{DescriptionsTotal}");
                    // Keep IsSyncPanelVisible = false (no active sync)
                }
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

        // Step 1.5: Filter by Played Games (if enabled)
        if (ShowOnlyPlayedGames)
        {
            filtered = filtered.Where(g => g.PlaytimeMinutes > 0);
        }

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

        // Notify UI that HasNoData property may have changed
        OnPropertyChanged(nameof(HasNoData));

        // NEW STEP 4: Auto-Selection for Centered Views
        // If we are in a mode that relies on centering, we MUST ensure the first
        // game is selected after a filter, otherwise the carousel stays empty or off-center.
        if (ActiveView == _coverView || ActiveView == _carouselView)
        {
            if (_allGames.Count > 0)
            {
                // Forces the carousel to snap to the first result of the search
                SelectedGame = _allGames[0];
            }
            else
            {
                SelectedGame = null;
            }
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

        // Validate that Steam credentials are configured
        if (string.IsNullOrWhiteSpace(Settings.GetApiKey()) || string.IsNullOrWhiteSpace(Settings.GetSteamId()))
        {
            ErrorMessage = "Steam API Key and Steam ID are required. Please update them in Settings before refreshing.";
            return;
        }

        try
        {
            IsRefreshing = true;
            ErrorMessage = string.Empty; // Clear any previous error messages


            // Reset sync progress
            GameListCompleted = false;
            ImagesCompleted = false;
            DescriptionsCompleted = false;
            GameListCurrent = 0;
            ImagesCurrent = 0;
            DescriptionsCurrent = 0;
            IsSyncPanelVisible = true;

            var freshGames = await _steamService.GetLibrarySkeletonAsync();

            if (freshGames != null && freshGames.Any())
            {
                _masterLibrary.Clear();
                _masterLibrary.AddRange(freshGames);
                ApplyFilteringAndSorting();

                // Set totals
                GameListTotal = freshGames.Count;
                ImagesTotal = freshGames.Count;
                DescriptionsTotal = freshGames.Count;

                // Mark game list as complete
                GameListCurrent = GameListTotal;
                GameListCompleted = true;

                // Save to disk and start fetching high-res images/descriptions in the background
                await _cacheService.SaveLibraryCacheAsync(_masterLibrary);
                _ = Task.Run(() => BackgroundUpdateDataAsync());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during refresh: {ex.Message}");
            ErrorMessage = $"Failed to refresh library: {ex.Message}";
            IsSyncPanelVisible = false;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Handles the secondary and tertiary stages of loading (Images and Descriptions).
    /// Updates the objects in the master library directly and reports progress.
    /// </summary>
    private async Task BackgroundUpdateDataAsync()
    {
        try
        {
            // Stage 1: Download Images
            var imageCount = 0;
            foreach (var game in _masterLibrary)
            {
                await _steamService.LoadGameImageAsync(game);
                imageCount++;
                ImagesCurrent = imageCount;
            }

            ImagesCompleted = true;
            await _cacheService.SaveLibraryCacheAsync(_masterLibrary);

            // Stage 2: Download Descriptions
            var descCount = 0;
            foreach (var game in _masterLibrary)
            {
                // Only fetch if empty or currently showing the placeholder/error state
                if (string.IsNullOrWhiteSpace(game.Description) ||
                    game.Description == "Loading description..." ||
                    game.Description == "No description available.")
                {
                    game.Description = await _steamService.GetGameDescriptionAsync(game.AppId);
                    descCount++;
                    DescriptionsCurrent = descCount;

                    // Save cache every 5 games
                    if (descCount % 5 == 0)
                    {
                        await _cacheService.SaveLibraryCacheAsync(_masterLibrary);
                    }

                    // Delay to avoid IP block
                    await Task.Delay(1500);
                }
                else
                {
                    // Count already existing descriptions
                    descCount++;
                    DescriptionsCurrent = descCount;
                }
            }


            DescriptionsCompleted = true;
            await _cacheService.SaveLibraryCacheAsync(_masterLibrary);

            // Mark sync as fully complete
            await _cacheService.SaveSyncStateAsync(true);
            System.Diagnostics.Debug.WriteLine("✅ Full sync completed!");

            // Note: IsSyncPanelVisible stays true so users can see final sync stats
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in background update: {ex.Message}");
            // Mark sync as incomplete so it resumes on next startup
            await _cacheService.SaveSyncStateAsync(false);
        }
    }
    /// Switches the active view mode and ensures a selection exists for centering-based views.
    /// </summary>
    [RelayCommand]
    public void SelectMode(string mode)
    {
        // Reset all mode flags
        IsListMode = false;
        IsGridMode = false;
        IsCoverMode = false;

        // Set active view and corresponding flag
        ActiveView = mode switch
        {
            "Grid" => _gridView,
            "Cover" => _coverView,
            "Carousel" => _carouselView,
            _ => _listView
        };

        // Update the mode flag based on which view was selected
        switch (mode)
        {
            case "Grid":
                IsGridMode = true;
                break;
            case "Cover":
                IsCoverMode = true;
                break;
            case "Carousel":
                // IsCarouselMode is computed from ActiveView, no need to set a flag
                break;
            default: // "List"
                IsListMode = true;
                break;
        }

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


    [RelayCommand]
    public void ToggleSyncPanel()
    {
        IsSyncPanelExpanded = !IsSyncPanelExpanded;
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = string.Empty;
    }
    /// <summary>
    /// Navigates to the details view for a specific game.
    /// </summary>
    [RelayCommand]
    public void OpenGameDetails(GameModel game)
    {
        var detailsVm = new GameDetailsViewModel(game, _cacheService)
        {
            RequestClose = () => IsGameDetailsOpen = false,
            GetParentWindow = GetMainWindow,
            OnImageChanged = async () =>
            {
                // Save the updated cache when image changes
                await _cacheService.SaveLibraryCacheAsync(_masterLibrary);
            }
        };
        CurrentDetails = detailsVm;
        IsGameDetailsOpen = true;
    }

    /// <summary>
    /// Gets the main application window for use in file picker dialogs.
    /// </summary>
    private Avalonia.Controls.Window? GetMainWindow()
    {
        // Get the main window from the application lifetime
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
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
        OnPropertyChanged(nameof(IsSortAlphabetical));
        OnPropertyChanged(nameof(IsSortPlayTime));
        OnPropertyChanged(nameof(IsSortAppId));
        ApplyFilteringAndSorting();
    }

    /// <summary>
    /// Clears all game data from memory.
    /// Called by SettingsViewModel after clearing cache files.
    /// </summary>
    public void ClearAllData()
    {
        _masterLibrary.Clear();
        _allGames.Clear();
        SelectedGame = null;
        OnPropertyChanged(nameof(HasNoData));
        System.Diagnostics.Debug.WriteLine("All game data cleared from memory");
    }
}