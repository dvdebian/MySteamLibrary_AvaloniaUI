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

    [ObservableProperty]
    private LibraryPresenterViewModel _activeView;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isGameDetailsOpen;

    // State to track if the background refresh is running
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
        _activeView = new ListViewModel { Games = _allGames };

        Settings.RequestClose = () => IsSettingsOpen = false;

        _ = LoadSteamLibraryAsync();
    }

    /// <summary>
    /// Background task to fetch initial Steam data or Cache.
    /// </summary>
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

    /// <summary>
    /// Manually triggers a deep refresh to fetch missing descriptions and images.
    /// This uses the rate-limited batch process in the SteamApiService.
    /// </summary>
    [RelayCommand]
    public async Task RefreshLibrary()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;

            // 1. Fetch missing descriptions (this will take time due to 1.5s delay per game)
            // It updates the objects inside _allGames directly.
            await _steamService.RefreshDescriptionsAsync(_allGames);

            // 2. Force a refresh of the current view to ensure all data is bound correctly
            // Using the current view type to refresh
            string currentType = ActiveView.GetType().Name.Replace("ViewModel", "");
            SelectMode(currentType);
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

    [RelayCommand]
    public void SelectMode(string mode)
    {
        ActiveView = mode switch
        {
            "Grid" => new GridViewModel { Games = _allGames },
            "Cover" => new CoverViewModel { Games = _allGames },
            "Carousel" => new CarouselViewModel { Games = _allGames },
            _ => new ListViewModel { Games = _allGames }
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