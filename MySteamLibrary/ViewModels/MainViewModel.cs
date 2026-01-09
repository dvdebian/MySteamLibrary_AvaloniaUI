using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySteamLibrary.Helpers;
using MySteamLibrary.Models;
using System.Collections.ObjectModel;
using MySteamLibrary.ViewModels;

namespace MySteamLibrary.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // The 'ActiveView' is typed as the base class (LibraryPresenterViewModel).
    // This allows it to hold any child class: List, Grid, Cover, or Carousel.
    [ObservableProperty]
    private LibraryPresenterViewModel _activeView;

    // Controls the visibility of the SettingsView overlay.
    [ObservableProperty]
    private bool _isSettingsOpen;

    // Stores the current search query from the TextBox.
    [ObservableProperty]
    private string _searchText = string.Empty;

    // Persistent instance of the Settings logic.
    public SettingsViewModel Settings { get; } = new();

    // The master list of games loaded from our Helper.
    private readonly ObservableCollection<GameModel> _allGames;

    public MainViewModel()
    {
        // 1. Load the dummy data once during initialization.
        _allGames = DummyDataService.GetFakeGames();

        // 2. Default to List View on startup.
        // We initialize the specific ViewModel and pass it the game list.
        _activeView = new ListViewModel { Games = _allGames };
    }

    /// <summary>
    /// Swaps the central content area by updating the ActiveView property.
    /// This is triggered by the buttons in MainView.axaml.
    /// </summary>
    [RelayCommand]
    public void SelectMode(string mode)
    {
        // We create a new instance of the requested view mode.
        // Each instance is assigned the same game collection.
        ActiveView = mode switch
        {
            "Grid" => new GridViewModel { Games = _allGames },
            "Cover" => new CoverViewModel { Games = _allGames },
            "Carousel" => new CarouselViewModel { Games = _allGames },
            _ => new ListViewModel { Games = _allGames }
        };
    }

    /// <summary>
    /// Toggles the Settings overlay boolean.
    /// </summary>
    [RelayCommand]
    public void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }
}