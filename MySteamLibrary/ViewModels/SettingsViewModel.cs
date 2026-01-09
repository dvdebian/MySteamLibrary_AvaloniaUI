using CommunityToolkit.Mvvm.ComponentModel;

namespace MySteamLibrary.ViewModels;

/// <summary>
/// Manages the configuration data for the application.
/// These properties are bound to the TextBoxes in SettingsView.axaml.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    // The Steam Web API Key required to fetch user data.
    // [ObservableProperty] allows the UI to update if the value changes in code.
    [ObservableProperty]
    private string _steamApiKey = string.Empty;

    // The unique 64-bit Steam ID for the user.
    [ObservableProperty]
    private string _steamId = string.Empty;

    // Example of an additional setting seen in many library managers:
    // The local path where Steam is installed.
    [ObservableProperty]
    private string _steamPath = @"C:\Program Files (x86)\Steam";

    public SettingsViewModel()
    {
        // For now, we initialize with empty strings or dummy values.
        // Later, we can add logic here to load these from a JSON file.
    }
}