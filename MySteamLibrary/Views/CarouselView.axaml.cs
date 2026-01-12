using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MySteamLibrary.ViewModels;
using MySteamLibrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MySteamLibrary.Views;

/// <summary>
/// Code-behind for CarouselView with ItemsRepeater. 
/// Handles magnetic centering and window resize recalculation.
/// </summary>
public partial class CarouselView : UserControl, INotifyPropertyChanged
{
    private int _selectedIndex = 0;
    private GameModel? _currentSelectedGame;

    public event PropertyChangedEventHandler? PropertyChanged;

    public GameModel? CurrentSelectedGame
    {
        get => _currentSelectedGame;
        set
        {
            if (_currentSelectedGame != value)
            {
                _currentSelectedGame = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSelectedGame)));
            }
        }
    }

    public CarouselView()
    {
        InitializeComponent();

        // Watch for selection changes from ViewModel
        DataContextChanged += OnDataContextChanged;

        // Listen for keyboard support
        CarouselScroller.KeyDown += OnKeyDown;

        // Ensure we center on load and grab focus
        AttachedToVisualTree += (s, e) =>
        {
            ScrollToSelected();
            Dispatcher.UIThread.Post(() => CarouselScroller.Focus(), DispatcherPriority.Background);
        };
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CarouselViewModel vm)
        {
            // Subscribe to selection changes
            vm.Parent.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.SelectedGame))
                {
                    CurrentSelectedGame = vm.Parent.SelectedGame;
                    UpdateSelectedIndex();
                    ScrollToSelected();
                }
            };

            // Initial setup - select first game if nothing is selected
            if (vm.Parent.SelectedGame == null && vm.Games != null)
            {
                var firstGame = vm.Games.FirstOrDefault();
                if (firstGame != null)
                {
                    vm.Parent.SelectedGame = firstGame;
                }
            }

            CurrentSelectedGame = vm.Parent.SelectedGame;
            UpdateSelectedIndex();

            // Delay initial scroll to ensure layout is ready
            Dispatcher.UIThread.Post(() => ScrollToSelected(), DispatcherPriority.Loaded);
        }
    }

    private void UpdateSelectedIndex()
    {
        if (DataContext is CarouselViewModel vm && vm.Parent.SelectedGame != null)
        {
            var games = vm.Games?.ToList();
            if (games != null)
            {
                _selectedIndex = games.IndexOf(vm.Parent.SelectedGame);
                if (_selectedIndex < 0) _selectedIndex = 0;
            }
        }
    }

    /// <summary>
    /// Handles clicking on a game cover.
    /// </summary>
    private void OnCoverClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is GameModel clickedGame)
        {
            if (DataContext is CarouselViewModel viewModel)
            {
                var games = viewModel.Games?.ToList();
                if (games == null) return;

                int clickedIndex = games.IndexOf(clickedGame);

                // If already selected, open details
                if (clickedIndex == _selectedIndex && viewModel.Parent.SelectedGame == clickedGame)
                {
                    viewModel.Parent.OpenGameDetailsCommand.Execute(clickedGame);
                    e.Handled = true;
                }
                else
                {
                    // Select this game
                    _selectedIndex = clickedIndex;
                    viewModel.Parent.SelectedGame = clickedGame;
                    CurrentSelectedGame = clickedGame;
                }
            }

            CarouselScroller.Focus();
        }
    }

    /// <summary>
    /// Handles keyboard actions.
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CarouselViewModel viewModel) return;
        var games = viewModel.Games?.ToList();
        if (games == null || games.Count == 0) return;

        bool handled = false;

        switch (e.Key)
        {
            case Key.Left:
                if (_selectedIndex > 0)
                {
                    _selectedIndex--;
                    handled = true;
                }
                break;

            case Key.Right:
                if (_selectedIndex < games.Count - 1)
                {
                    _selectedIndex++;
                    handled = true;
                }
                break;

            case Key.Enter:
                if (_selectedIndex >= 0 && _selectedIndex < games.Count)
                {
                    viewModel.Parent.OpenGameDetailsCommand.Execute(games[_selectedIndex]);
                    handled = true;
                }
                break;
        }

        if (handled)
        {
            viewModel.Parent.SelectedGame = games[_selectedIndex];
            CurrentSelectedGame = games[_selectedIndex];
            e.Handled = true;
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // Get the MainGrid and update row height dynamically
        if (this.FindControl<Grid>("MainGrid") is Grid mainGrid && mainGrid.RowDefinitions.Count > 0)
        {
            // Calculate dynamic row height and margin based on window height
            double height = e.NewSize.Height;
            double minHeight = 500;
            double maxHeight = 1080;

            double clampedHeight = Math.Clamp(height, minHeight, maxHeight);
            double ratio = (clampedHeight - minHeight) / (maxHeight - minHeight);

            // Row height: 0px for small, 180px for large
            double rowHeight = 0 + (ratio * 180);
            mainGrid.RowDefinitions[0] = new RowDefinition(rowHeight, GridUnitType.Pixel);

            // ScrollViewer negative margin: -80px for small, 0px for large
            // This pulls games UP above the row boundary for small windows
            double scrollMargin = -80 + (ratio * 80);
            CarouselScroller.Margin = new Thickness(0, scrollMargin, 0, 0);
        }

        // Force immediate recalculation by temporarily resetting offset
        CarouselScroller.Offset = new Vector(0, 0);

        // Then recalculate and apply the correct offset
        Dispatcher.UIThread.Post(() =>
        {
            ScrollToSelected();
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Centers the selected item within the ScrollViewer.
    /// </summary>
    private void ScrollToSelected()
    {
        if (DataContext is not CarouselViewModel vm || vm.Games == null) return;

        var games = vm.Games.ToList();
        if (_selectedIndex < 0 || _selectedIndex >= games.Count) return;

        Dispatcher.UIThread.Post(() =>
        {
            // Item dimensions - must match XAML exactly
            double itemWidth = 220;
            double itemMargin = 10; // 5px on each side
            double stackSpacing = 10; // From StackLayout Spacing property
            double totalItemSpacing = itemMargin + stackSpacing; // Distance between item centers

            // The CenterPaddingConverter adds padding = (viewport/2 - itemWidth/2)
            double viewportWidth = CarouselScroller.Viewport.Width;
            double centerPadding = Math.Max(0, (viewportWidth / 2) - (itemWidth / 2));

            // Position of the selected item's left edge (including padding)
            double itemLeftPosition = centerPadding + (_selectedIndex * (itemWidth + totalItemSpacing));

            // Center of the selected item
            double itemCenterPosition = itemLeftPosition + (itemWidth / 2);

            // We want the item center at the viewport center
            double viewportCenter = viewportWidth / 2;
            double targetOffset = itemCenterPosition - viewportCenter;

            // Apply with bounds checking
            CarouselScroller.Offset = new Vector(Math.Max(0, targetOffset), 0);

        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Handles the mouse wheel to navigate items.
    /// </summary>
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not CarouselViewModel viewModel) return;
        var games = viewModel.Games?.ToList();
        if (games == null || games.Count == 0) return;

        if (e.Delta.Y < 0)
        {
            if (_selectedIndex < games.Count - 1)
                _selectedIndex++;
        }
        else
        {
            if (_selectedIndex > 0)
                _selectedIndex--;
        }

        viewModel.Parent.SelectedGame = games[_selectedIndex];
        CurrentSelectedGame = games[_selectedIndex];

        e.Handled = true;
        CarouselScroller.Focus();
    }
}