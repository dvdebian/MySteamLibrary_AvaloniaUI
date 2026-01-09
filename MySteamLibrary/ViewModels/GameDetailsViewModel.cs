using CommunityToolkit.Mvvm.ComponentModel;
using MySteamLibrary.Models;

namespace MySteamLibrary.ViewModels;

public partial class GameDetailsViewModel : ViewModelBase
{
    [ObservableProperty]
    private GameModel? _selectedGame;

    public GameDetailsViewModel(GameModel game)
    {
        SelectedGame = game;
    }
}