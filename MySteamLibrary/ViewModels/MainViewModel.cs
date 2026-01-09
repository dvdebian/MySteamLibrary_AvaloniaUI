using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySteamLibrary.Models;
using MySteamLibrary.Services;
using MySteamLibrary.ViewModels;

namespace MySteamLibrary.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SteamApiService _steamService = new();

    // 1. Fields to store the pre-created view models
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

    private readonly ObservableCollection<GameModel> _allGames = new();

    public MainViewModel()
    {
        // 2. Initialize each view model once, passing the master game collection
        _listView = new ListViewModel { Games = _allGames };
        _gridView = new GridViewModel { Games = _allGames };
        _coverView = new CoverViewModel { Games = _allGames };
        _carouselView = new CarouselViewModel { Games = _allGames };

        // Set initial view reference
        _activeView = _listView;

        Settings.RequestClose = () => IsSettingsOpen = false;

        _ = LoadSteamLibraryAsync();
    }

    private async Task LoadSteamLibraryAsync()
    {
        try
        {
            var result = await _steamService.GetFullLibraryAsync();

            _allGames.Clear();
            foreach (var game in result)
            {
                _allGames.Add(game);
            }

            SelectMode("List");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load library: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RefreshLibrary()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            await _steamService.RefreshDescriptionsAsync(_allGames);

            // Refresh logic: Since instances are cached, no need to re-create
            // UI bindings will update via ObservableProperty in GameModel
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
    /// Swaps the ActiveView to a pre-existing instance instead of creating a 'new' one.
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