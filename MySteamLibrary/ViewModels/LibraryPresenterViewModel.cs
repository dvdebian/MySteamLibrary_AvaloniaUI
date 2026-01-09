using System.Collections.ObjectModel;
using MySteamLibrary.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MySteamLibrary.ViewModels;

public partial class LibraryPresenterViewModel : ViewModelBase
{
    // The collection of games shared by all views
    public ObservableCollection<GameModel> Games { get; set; } = new();

    // The name of the specific mode (e.g., "List", "Carousel")
    [ObservableProperty]
    private string _modeName = string.Empty;
}