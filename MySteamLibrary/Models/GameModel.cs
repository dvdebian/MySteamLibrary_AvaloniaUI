using CommunityToolkit.Mvvm.ComponentModel;

namespace MySteamLibrary.Models
{
    /// <summary>
    /// The core data structure for a single game.
    /// Updated to use ObservableProperty for automatic UI updates.
    /// </summary>
    public partial class GameModel : ObservableObject
    {
        // The unique Steam Application ID
        [ObservableProperty]
        private int _appId;

        // The display name of the game
        [ObservableProperty]
        private string _title = string.Empty;

        // The full game description fetched from the Store API
        // Updated: Set to "Loading..." by default for immediate UI feedback
        [ObservableProperty]
        private string _description = "Loading description...";

        // Raw playtime in minutes as returned by the Steam API
        // NotifyPropertyChangedFor ensures the PlayTime string updates when this value changes
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PlayTime))]
        private int _playtimeMinutes;

        // The file path or URL for the game cover/poster image
        [ObservableProperty]
        private string _imagePath = string.Empty;

        /// <summary>
        /// Formatted string for playtime (e.g., "45.2 hours").
        /// This is a calculated property based on PlaytimeMinutes.
        /// </summary>
        public string PlayTime => $"{(_playtimeMinutes / 60.0):F1} hours";
    }
}