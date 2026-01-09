using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace MySteamLibrary.ViewModels;

/// <summary>
/// Manages the configuration data for the application.
/// These properties are bound to the TextBoxes in SettingsView.axaml.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    // A delegate (callback) that the MainViewModel will provide.
    // This allows this ViewModel to request a close without knowing about MainViewModel.
    public Action? RequestClose { get; set; }

    // The Steam Web API Key required to fetch user data.
    [ObservableProperty]
    private string _steamApiKey = string.Empty;

    // The unique 64-bit Steam ID for the user.
    [ObservableProperty]
    private string _steamId = string.Empty;

    // The local path where Steam is installed.
    [ObservableProperty]
    private string _steamPath = @"C:\Program Files (x86)\Steam";

    public SettingsViewModel()
    {
        // Initialization logic can go here.
    }

    /// <summary>
    /// Triggered by the Close button in the UI.
    /// It executes the RequestClose action if one has been assigned by the parent.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        // The ?. operator ensures we only call Invoke if RequestClose is not null.
        RequestClose?.Invoke();
    }
}