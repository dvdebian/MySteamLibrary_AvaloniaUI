using CommunityToolkit.Mvvm.ComponentModel;
using MySteamLibrary.Models;

namespace MySteamLibrary.ViewModels
{
    /// <summary>
    /// Presenter for the Cover/Carousel view.
    /// Selection logic is centralized in MainViewModel. ApplyFilteringAndSorting
    /// to ensure consistent behavior across all view modes.
    /// </summary>
    public partial class CoverViewModel : LibraryPresenterViewModel
    {
        // This property can remain if you need view-specific selection logic later,
        // but currently, the UI binds directly to Parent.SelectedGame.
        [ObservableProperty]
        private GameModel? _selectedGame;

        public CoverViewModel()
        {
            // Constructor is now clean. 
            // Selection is handled by MainViewModel during initialization and filtering.
        }
    }
}