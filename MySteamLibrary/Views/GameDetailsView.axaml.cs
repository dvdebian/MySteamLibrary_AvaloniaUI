using Avalonia.Controls;

namespace MySteamLibrary.Views;

/// <summary>
/// Interaction logic for the GameDetailsView.
/// This view is responsible for displaying extended information about a specific game
/// and is usually shown as an overlay on top of the main library.
/// </summary>
public partial class GameDetailsView : UserControl
{
    public GameDetailsView()
    {
        // Standard Avalonia method that parses the XAML file 
        // and connects it to this class.
        InitializeComponent();
    }
}