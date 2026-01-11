using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MySteamLibrary.ViewModels;
using MySteamLibrary.Models;
using System;

namespace MySteamLibrary.Views;

/// <summary>
/// Code-behind for CarouselView. 
/// Handles magnetic centering and window resize recalculation.
/// </summary>
public partial class CarouselView : UserControl
{
    public CarouselView()
    {
        InitializeComponent();

        // Trigger centering whenever the selection changes
        // Using CarouselList which is the name we gave it in the CarouselView.axaml
        CarouselList.SelectionChanged += OnSelectionChanged;

        // Listen for keyboard support
        CarouselList.KeyDown += OnKeyDown;

        // Ensure we center on load and grab focus so Arrow Keys work immediately
        AttachedToVisualTree += (s, e) =>
        {
            ScrollToSelected();

            // Focus the list for Arrow keys to respond immediately on view load
            Dispatcher.UIThread.Post(() => CarouselList.Focus(), DispatcherPriority.Background);
        };
    }

    /// <summary>
    /// Handles clicking on a game cover.
    /// </summary>
    private void OnCoverClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is GameModel clickedGame)
        {
            // If already selected, open details
            if (CarouselList.SelectedItem == clickedGame)
            {
                if (DataContext is CoverViewModel viewModel)
                {
                    viewModel.Parent.OpenGameDetailsCommand.Execute(clickedGame);
                    e.Handled = true;
                }
            }
            // Ensure list maintains focus for keyboard continuity
            CarouselList.Focus();
        }
    }

    /// <summary>
    /// Handles keyboard actions.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && CarouselList.SelectedItem is GameModel selectedGame)
        {
            if (DataContext is CoverViewModel viewModel)
            {
                viewModel.Parent.OpenGameDetailsCommand.Execute(selectedGame);
                e.Handled = true;
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ScrollToSelected();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ScrollToSelected();
    }

    /// <summary>
    /// Centers the selected item within the ScrollViewer.
    /// </summary>
    private void ScrollToSelected()
    {
        var selectedItem = CarouselList.SelectedItem;
        if (selectedItem == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var scrollViewer = CarouselList.GetValue(ListBox.ScrollProperty) as ScrollViewer
                                ?? CarouselList.FindControl<ScrollViewer>("PART_ScrollViewer");

            if (scrollViewer == null) return;

            var container = CarouselList.ContainerFromItem(selectedItem);

            if (container != null)
            {
                var containerCenter = container.Bounds.Width / 2;
                var relativePoint = container.TranslatePoint(new Point(containerCenter, 0), CarouselList);

                if (relativePoint.HasValue)
                {
                    double screenCenter = CarouselList.Bounds.Width / 2;
                    double driftError = relativePoint.Value.X - screenCenter;

                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X + driftError, 0);
                }
            }
            else
            {
                // Fallback if container isn't ready yet
                CarouselList.ScrollIntoView(selectedItem);
                Dispatcher.UIThread.Post(ScrollToSelected, DispatcherPriority.Background);
            }
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Handles the mouse wheel to navigate items.
    /// </summary>
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y < 0)
        {
            if (CarouselList.SelectedIndex < CarouselList.ItemCount - 1)
                CarouselList.SelectedIndex++;
        }
        else
        {
            if (CarouselList.SelectedIndex > 0)
                CarouselList.SelectedIndex--;
        }

        e.Handled = true;
        // Keep focus on the list when using the wheel
        CarouselList.Focus();
    }
}