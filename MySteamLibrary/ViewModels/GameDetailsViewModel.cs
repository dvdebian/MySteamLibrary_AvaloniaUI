using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySteamLibrary.Models;
using System;

namespace MySteamLibrary.ViewModels;

public partial class GameDetailsViewModel : ViewModelBase
{
    // Callback for the parent to handle closing this view
    public Action? RequestClose { get; set; }

    [ObservableProperty]
    private GameModel? _selectedGame;

    public GameDetailsViewModel(GameModel game)
    {
        SelectedGame = game;
    }

    /// <summary>
    /// Executes when the user clicks the "BACK" button.
    /// Notifies the parent (MainViewModel) to hide this overlay.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}