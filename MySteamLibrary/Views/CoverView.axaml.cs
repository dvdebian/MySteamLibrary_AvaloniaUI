using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MySteamLibrary.ViewModels;
using MySteamLibrary.Models;
using System;

namespace MySteamLibrary.Views;

/// <summary>
/// Code-behind for CoverView. 
/// Handles magnetic centering and window resize recalculation.
/// </summary>
public partial class CoverView : UserControl
{
    public CoverView()
    {
        InitializeComponent();

        // Trigger centering whenever the selection changes
        CoverList.SelectionChanged += OnSelectionChanged;

        // Listen for keyboard (for future Enter support, but clean for now)
        CoverList.KeyDown += OnKeyDown;

        // Ensure we center on load and grab focus so Arrow Keys work immediately
        AttachedToVisualTree += (s, e) =>
        {
            ScrollToSelected();

            // This is the trick: we must focus the list for Arrow keys to respond
            Dispatcher.UIThread.Post(() => CoverList.Focus(), DispatcherPriority.Background);
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
            if (CoverList.SelectedItem == clickedGame)
            {
                if (DataContext is CoverViewModel viewModel)
                {
                    viewModel.Parent.OpenGameDetailsCommand.Execute(clickedGame);
                    e.Handled = true;
                }
            }
            // If not selected, focusing the list ensures keyboard continues working
            CoverList.Focus();
        }
    }

    /// <summary>
    /// Placeholder for keyboard actions.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // We leave this empty for now so it doesn't block Arrow Keys
        // Default ListBox behavior will handle Left/Right arrows automatically 
        // as long as the ListBox is Focused.

        if (e.Key == Key.Enter && CoverList.SelectedItem is GameModel selectedGame)
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

    private void ScrollToSelected()
    {
        var selectedItem = CoverList.SelectedItem;
        if (selectedItem == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var scrollViewer = CoverList.GetValue(ListBox.ScrollProperty) as ScrollViewer
                               ?? CoverList.FindControl<ScrollViewer>("PART_ScrollViewer");

            if (scrollViewer == null) return;

            var container = CoverList.ContainerFromItem(selectedItem);

            if (container != null)
            {
                var containerCenter = container.Bounds.Width / 2;
                var relativePoint = container.TranslatePoint(new Point(containerCenter, 0), CoverList);

                if (relativePoint.HasValue)
                {
                    double screenCenter = CoverList.Bounds.Width / 2;
                    double driftError = relativePoint.Value.X - screenCenter;

                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X + driftError, 0);
                }
            }
            else
            {
                CoverList.ScrollIntoView(selectedItem);
                Dispatcher.UIThread.Post(ScrollToSelected, DispatcherPriority.Background);
            }
        }, DispatcherPriority.Render);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y < 0)
        {
            if (CoverList.SelectedIndex < CoverList.ItemCount - 1)
                CoverList.SelectedIndex++;
        }
        else
        {
            if (CoverList.SelectedIndex > 0)
                CoverList.SelectedIndex--;
        }

        e.Handled = true;
        // Keep focus on the list when using the wheel
        CoverList.Focus();
    }
}