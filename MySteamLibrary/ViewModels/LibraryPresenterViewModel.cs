using System.Collections.ObjectModel;
using MySteamLibrary.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MySteamLibrary.ViewModels;

/// <summary>
/// Common base for all ViewModels that display game lists.
/// Updated to support global selection tracking via the Parent reference.
/// </summary>
public partial class LibraryPresenterViewModel : ViewModelBase
{
    // The collection of games shared by all views
    public ObservableCollection<GameModel> Games { get; set; } = new();

    // The name of the specific mode (e.g., "List", "Carousel")
    [ObservableProperty]
    private string _modeName = string.Empty;

    /// <summary>
    /// Reference to the MainViewModel to access global state like SelectedGame.
    /// This is set during initialization in MainViewModel's constructor.
    /// </summary>
    [ObservableProperty]
    private MainViewModel? _parent;
}