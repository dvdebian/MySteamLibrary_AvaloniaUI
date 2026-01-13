using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MySteamLibrary.ViewModels;
using System.Linq;

namespace MySteamLibrary.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void OnEffectButtonClicked(object? sender, RoutedEventArgs e)
    {
        // Find the CarouselView in the visual tree
        var contentControl = this.FindControl<ContentControl>("ContentControl");
        if (contentControl == null)
        {
            // Try alternative method - find by walking the visual tree
            var content = this.GetVisualDescendants().OfType<ContentControl>().FirstOrDefault();
            if (content?.Content is CarouselView carouselView)
            {
                carouselView.ToggleEffectOverlay();
                return;
            }
        }

        // If ContentControl found, check its content
        if (contentControl?.Content is CarouselView carousel)
        {
            carousel.ToggleEffectOverlay();
        }
        else
        {
            // Last resort: find any CarouselView in the visual tree
            var foundCarousel = this.GetVisualDescendants().OfType<CarouselView>().FirstOrDefault();
            foundCarousel?.ToggleEffectOverlay();
        }
    }
}