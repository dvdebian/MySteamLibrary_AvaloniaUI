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

    // --- NEW PROPERTIES FOR GAME DETAILS ---

    // Controls the visibility of the GameDetails overlay.
    [ObservableProperty]
    private bool _isGameDetailsOpen;

    // Holds the specific ViewModel instance for the game currently being viewed.
    // When this is null, no game details are being processed.
    [ObservableProperty]
    private GameDetailsViewModel? _currentDetails;

    // ----------------------------------------

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
        _activeView = new ListViewModel { Games = _allGames };

        // Set up the "Callback". 
        // We tell the SettingsViewModel: "When you call RequestClose, 
        // I will run my ToggleSettings logic."
        Settings.RequestClose = () => IsSettingsOpen = false;
    }

    /// <summary>
    /// Swaps the central content area by updating the ActiveView property.
    /// This is triggered by the buttons in MainView.axaml.
    /// </summary>
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

    /// <summary>
    /// Toggles the Settings overlay boolean.
    /// </summary>
    [RelayCommand]
    public void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    // --- NEW COMMANDS FOR GAME DETAILS ---

    /// <summary>
    /// Creates a new GameDetailsViewModel for the selected game and shows the overlay.
    /// This will be called from your List, Grid, or Carousel views.
    /// </summary>
    [RelayCommand]
    public void OpenGameDetails(GameModel game)
    {
        // 1. Create the instance
        var detailsVm = new GameDetailsViewModel(game);

        // 2. Hook up the Close logic (Best Practice)
        detailsVm.RequestClose = () => IsGameDetailsOpen = false;

        // 3. Assign it to the property and show the overlay
        CurrentDetails = detailsVm;
        IsGameDetailsOpen = true;
    }

    /// <summary>
    /// Closes the details overlay.
    /// </summary>
    [RelayCommand]
    public void CloseGameDetails()
    {
        IsGameDetailsOpen = false;
        // Optional: Clear the memory by setting the details VM to null.
        CurrentDetails = null;
    }
}