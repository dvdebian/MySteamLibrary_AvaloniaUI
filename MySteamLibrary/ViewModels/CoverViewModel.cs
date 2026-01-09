using CommunityToolkit.Mvvm.ComponentModel;
using MySteamLibrary.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace MySteamLibrary.ViewModels
{
    public partial class CoverViewModel : LibraryPresenterViewModel
    {
        [ObservableProperty]
        private GameModel? _selectedGame;

        public CoverViewModel()
        {
            // Initializing with the first game if available
            if (Games.Any())
            {
                SelectedGame = Games.First();
            }
        }
    }
}