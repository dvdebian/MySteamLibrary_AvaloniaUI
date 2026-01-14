using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MySteamLibrary.ViewModels;
using MySteamLibrary.Models;
using MySteamLibrary.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MySteamLibrary.Views;

/// <summary>
/// Code-behind for CarouselView with ItemsRepeater. 
/// Handles magnetic centering, window resize recalculation, and effect selection overlay.
/// </summary>
public partial class CarouselView : UserControl, INotifyPropertyChanged
{
    private int _selectedIndex = 0;
    private GameModel? _currentSelectedGame;
    private bool _isEffectOverlayVisible;
    private CarouselEffect _currentEffect;

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

    public bool IsEffectOverlayVisible
    {
        get => _isEffectOverlayVisible;
        set
        {
            if (_isEffectOverlayVisible != value)
            {
                _isEffectOverlayVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEffectOverlayVisible)));
            }
        }
    }

    public CarouselEffect CurrentEffect
    {
        get => _currentEffect;
        set
        {
            if (_currentEffect != value)
            {
                _currentEffect = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEffect)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEffectName)));
            }
        }
    }

    public string CurrentEffectName => FormatEffectName(CurrentEffect);

    public CarouselView()
    {
        InitializeComponent();

        // Initialize with the current effect from the converter
        CurrentEffect = CarouselTransformConverter.CurrentMode;

        DataContextChanged += OnDataContextChanged;
        CarouselScroller.KeyDown += OnKeyDown;

        AttachedToVisualTree += (s, e) =>
        {
            ScrollToSelected();
            Dispatcher.UIThread.Post(() => CarouselScroller.Focus(), DispatcherPriority.Background);
        };
    }

    private string FormatEffectName(CarouselEffect effect)
    {
        // Convert camelCase to spaced words
        string name = effect.ToString();
        string result = string.Empty;

        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                result += " ";
            }
            result += name[i];
        }

        return result.ToUpper();
    }

    public void ToggleEffectOverlay()
    {
        IsEffectOverlayVisible = !IsEffectOverlayVisible;
    }

    public void PreviousEffect()
    {
        var effects = Enum.GetValues<CarouselEffect>();
        int currentIndex = Array.IndexOf(effects, CurrentEffect);
        int newIndex = (currentIndex - 1 + effects.Length) % effects.Length;

        CurrentEffect = effects[newIndex];
        CarouselTransformConverter.CurrentMode = CurrentEffect;

        // Force refresh of the visual transforms
        RefreshCarouselTransforms();
    }

    public void NextEffect()
    {
        var effects = Enum.GetValues<CarouselEffect>();
        int currentIndex = Array.IndexOf(effects, CurrentEffect);
        int newIndex = (currentIndex + 1) % effects.Length;

        CurrentEffect = effects[newIndex];
        CarouselTransformConverter.CurrentMode = CurrentEffect;

        // Force refresh of the visual transforms
        RefreshCarouselTransforms();
    }

    private void RefreshCarouselTransforms()
    {
        // Trigger a re-evaluation of the bindings by temporarily changing selection
        var temp = CurrentSelectedGame;
        CurrentSelectedGame = null;
        Dispatcher.UIThread.Post(() =>
        {
            CurrentSelectedGame = temp;
        }, DispatcherPriority.Background);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CarouselViewModel vm)
        {
            vm.Parent.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.SelectedGame))
                {
                    CurrentSelectedGame = vm.Parent.SelectedGame;
                    UpdateSelectedIndex();
                    ScrollToSelected();
                }
            };

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

    private void OnCoverClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is GameModel clickedGame)
        {
            if (DataContext is CarouselViewModel viewModel)
            {
                var games = viewModel.Games?.ToList();
                if (games == null) return;

                int clickedIndex = games.IndexOf(clickedGame);

                if (clickedIndex == _selectedIndex && viewModel.Parent.SelectedGame == clickedGame)
                {
                    viewModel.Parent.OpenGameDetailsCommand.Execute(clickedGame);
                    e.Handled = true;
                }
                else
                {
                    _selectedIndex = clickedIndex;
                    viewModel.Parent.SelectedGame = clickedGame;
                    CurrentSelectedGame = clickedGame;
                }
            }

            CarouselScroller.Focus();
        }
    }

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

        if (this.FindControl<Grid>("MainGrid") is Grid mainGrid && mainGrid.RowDefinitions.Count > 0)
        {
            double height = e.NewSize.Height;
            double minHeight = 500;
            double maxHeight = 1080;

            double clampedHeight = Math.Clamp(height, minHeight, maxHeight);
            double ratio = (clampedHeight - minHeight) / (maxHeight - minHeight);

            double rowHeight = 0 + (ratio * 180);
            mainGrid.RowDefinitions[0] = new RowDefinition(rowHeight, GridUnitType.Pixel);

            double scrollMargin = -90 + (ratio * 90);
            CarouselScroller.Margin = new Thickness(0, scrollMargin, 0, 15);
        }

        CarouselScroller.Offset = new Vector(0, 0);

        Dispatcher.UIThread.Post(() =>
        {
            ScrollToSelected();
        }, DispatcherPriority.Render);
    }

    private void ScrollToSelected()
    {
        if (DataContext is not CarouselViewModel vm || vm.Games == null) return;

        var games = vm.Games.ToList();
        if (_selectedIndex < 0 || _selectedIndex >= games.Count) return;

        Dispatcher.UIThread.Post(() =>
        {
            double itemWidth = 220;
            double itemMargin = 10;
            double stackSpacing = 10;
            double totalItemSpacing = itemMargin + stackSpacing;

            double viewportWidth = CarouselScroller.Viewport.Width;
            double centerPadding = Math.Max(0, (viewportWidth / 2) - (itemWidth / 2));

            double itemLeftPosition = centerPadding + (_selectedIndex * (itemWidth + totalItemSpacing));
            double itemCenterPosition = itemLeftPosition + (itemWidth / 2);

            double viewportCenter = viewportWidth / 2;
            double targetOffset = itemCenterPosition - viewportCenter;

            CarouselScroller.Offset = new Vector(Math.Max(0, targetOffset), 0);

        }, DispatcherPriority.Background);
    }

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

    // Add these event handler methods to the CarouselView.axaml.cs file:

    private void OnEffectToggleClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ToggleEffectOverlay();
    }

    private void OnPreviousEffectClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PreviousEffect();
    }

    private void OnNextEffectClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NextEffect();
    }
}