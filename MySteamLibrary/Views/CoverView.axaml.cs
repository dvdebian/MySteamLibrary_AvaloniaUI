using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MySteamLibrary.ViewModels;
using System;

namespace MySteamLibrary.Views;

/// <summary>
/// Code-behind for CoverView. 
/// Handles magnetic centering and window resize recalculation using OnSizeChanged.
/// </summary>
public partial class CoverView : UserControl
{
    public CoverView()
    {
        InitializeComponent();

        // Trigger centering whenever the selection changes
        CoverList.SelectionChanged += OnSelectionChanged;

        // Ensure we center on the very first load
        AttachedToVisualTree += (s, e) => ScrollToSelected();
    }

    /// <summary>
    /// This method fires automatically whenever the control is resized.
    /// It ensures the selected game remains centered during window resizing.
    /// </summary>
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ScrollToSelected();
    }

    /// <summary>
    /// Event handler for selection changes.
    /// </summary>
    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ScrollToSelected();
    }

    /// <summary>
    /// Recalculates the scroll offset to ensure the selected item stays perfectly centered.
    /// </summary>
    private void ScrollToSelected()
    {
        var selectedItem = CoverList.SelectedItem;
        if (selectedItem == null) return;

        // Using DispatcherPriority.Render ensures we calculate after the layout is finalized
        Dispatcher.UIThread.Post(() =>
        {
            var scrollViewer = CoverList.GetValue(ListBox.ScrollProperty) as ScrollViewer
                               ?? CoverList.FindControl<ScrollViewer>("PART_ScrollViewer");

            if (scrollViewer == null) return;

            // Get the physical container (Visual) of the selected item
            var container = CoverList.ContainerFromItem(selectedItem);

            if (container != null)
            {
                // Find the midpoint of the card (usually 220 / 2 = 110)
                var containerCenter = container.Bounds.Width / 2;

                // Translate that midpoint relative to the ListBox coordinate system
                var relativePoint = container.TranslatePoint(new Point(containerCenter, 0), CoverList);

                if (relativePoint.HasValue)
                {
                    // Calculate the distance from the current card center to the screen center
                    double screenCenter = CoverList.Bounds.Width / 2;
                    double driftError = relativePoint.Value.X - screenCenter;

                    // Apply the corrective nudge to the current ScrollViewer offset
                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X + driftError, 0);
                }
            }
            else
            {
                // If the item is virtualized (not rendered), bring it into view and try centering again
                CoverList.ScrollIntoView(selectedItem);
                Dispatcher.UIThread.Post(ScrollToSelected, DispatcherPriority.Background);
            }
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Redirects mouse wheel to selection changes to keep the carousel feeling "locked".
    /// </summary>
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Scroll down / Wheel down = Move Right
        if (e.Delta.Y < 0)
        {
            if (CoverList.SelectedIndex < CoverList.ItemCount - 1)
                CoverList.SelectedIndex++;
        }
        else // Scroll up / Wheel up = Move Left
        {
            if (CoverList.SelectedIndex > 0)
                CoverList.SelectedIndex--;
        }

        e.Handled = true;
    }
}