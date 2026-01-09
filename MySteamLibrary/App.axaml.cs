using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MySteamLibrary.ViewModels;
using MySteamLibrary.Views;

namespace MySteamLibrary;

public partial class App : Application
{
    public override void Initialize()
    {
        // Loads the XAML definitions from App.axaml
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Check if the app is running on a Desktop environment
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. Create the MainViewModel (The "Brain")
            var viewModel = new MainViewModel();

            // 2. Assign the MainWindow and set its DataContext
            // This ensures all bindings in MainView.axaml have a source to talk to.
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }
        // Mobile/SingleView support (if applicable)
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}