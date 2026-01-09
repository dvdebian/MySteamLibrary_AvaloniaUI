namespace MySteamLibrary.Models;

/// <summary>
/// The core data structure for a single game.
/// This is used by all ViewModels to ensure data consistency.
/// </summary>
public class GameModel
{
    // The display name of the game
    public string Title { get; set; } = string.Empty;

    // A short summary for the List and Cover views
    public string Description { get; set; } = string.Empty;

    // Display string for time played (e.g. "45 hours")
    public string PlayTime { get; set; } = string.Empty;

    // The file path or URL for the game cover/poster image
    public string ImagePath { get; set; } = string.Empty;

    // The unique Steam Application ID
    public int AppId { get; set; }
}