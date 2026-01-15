using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MySteamLibrary.Converters;
using MySteamLibrary.Models;
using MySteamLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

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
    private System.Timers.Timer? _scrollTimer;
    private Image? _backgroundImage1;
    private Image? _backgroundImage2;
    private bool _isImage1Active = true;
    private string? _lastImagePath;
    private bool _isAnimating = false;
    private readonly BitmapValueConverter _imageConverter = new();

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
                OnBackgroundImageChanged();
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

        // Load saved effect from settings (will be overridden in DataContextChanged if DataContext exists)
        CurrentEffect = CarouselTransformConverter.CurrentMode;

        DataContextChanged += OnDataContextChanged;
        CarouselScroller.KeyDown += OnKeyDown;

        // NEW: Subscribe to events for title position updates
        PropertyChanged += OnPropertyChangedForTitle;

        // Only update on actual window resize (not during scroll)
        SizeChanged += OnSizeChangedForTitle;
        // DON'T use LayoutUpdated - it fires too often during scroll!

        AttachedToVisualTree += (s, e) =>
        {
            ScrollToSelected();

            // Update title position after a delay to let layout settle
            Dispatcher.UIThread.Post(() => {
                UpdateTitlePosition();
            }, DispatcherPriority.Loaded);

            Dispatcher.UIThread.Post(() => CarouselScroller.Focus(), DispatcherPriority.Background);

            // Setup dual background images for crossfade
            Loaded += (s, e) =>
            {
                _backgroundImage1 = this.FindControl<Image>("BackgroundImage1");
                _backgroundImage2 = this.FindControl<Image>("BackgroundImage2");

                // Set initial image
                var initialPath = CurrentSelectedGame?.ImagePath;
                if (initialPath != null && _backgroundImage1 != null)
                {
                    _backgroundImage1.Source = _imageConverter.Convert(initialPath, typeof(Bitmap), null, null) as Bitmap;
                    _backgroundImage1.Opacity = 1.0;
                    _lastImagePath = initialPath;
                }

                if (_backgroundImage2 != null)
                {
                    _backgroundImage2.Opacity = 0.0;
                }
            };

            Unloaded += (s, e) =>
            {
                // Cleanup if needed
            };
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

        // Save the selected effect to settings
        if (DataContext is CarouselViewModel vm)
        {
            vm.Parent.Settings.SelectedCarouselEffect = CurrentEffect;
            _ = vm.Parent.Settings.SaveSettingsAsync();
        }
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
        // Save the selected effect to settings
        if (DataContext is CarouselViewModel vm)
        {
            vm.Parent.Settings.SelectedCarouselEffect = CurrentEffect;
            _ = vm.Parent.Settings.SaveSettingsAsync();
        }
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
        // The 'if' starts here. We open a brace to group all logic that depends on 'vm'
        if (DataContext is CarouselViewModel vm)
        {
            // 1. Load saved carousel effect from settings
            CurrentEffect = vm.Parent.Settings.SelectedCarouselEffect;
            CarouselTransformConverter.CurrentMode = CurrentEffect;

            // 2. Subscribe to property changes on the Parent (MainViewModel)
            vm.Parent.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.SelectedGame))
                {
                    CurrentSelectedGame = vm.Parent.SelectedGame;
                    UpdateSelectedIndex();
                    ScrollToSelected();
                }
            };

            // 3. Ensure a game is selected if none is currently active
            if (vm.Parent.SelectedGame == null && vm.Games != null)
            {
                var firstGame = vm.Games.FirstOrDefault();
                if (firstGame != null)
                {
                    vm.Parent.SelectedGame = firstGame;
                }
            }

            // 4. Initialize the current state
            CurrentSelectedGame = vm.Parent.SelectedGame;
            UpdateSelectedIndex();

            // 5. Scroll to the selected item once the UI is loaded
            Dispatcher.UIThread.Post(() => ScrollToSelected(), DispatcherPriority.Loaded);
        } // This closing brace ends the scope of 'vm'
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

        // Reset scroll position
        CarouselScroller.Offset = new Vector(0, 0);

        // Scroll to selected card
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

    // ===== NEW: PHASE 2 - TITLE POSITIONING =====


    private void OnSizeChangedForTitle(object? sender, SizeChangedEventArgs e)
    {
        // Update title position on window resize
        UpdateTitlePosition();
    }

    private void OnPropertyChangedForTitle(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentSelectedGame))
        {
            // Stop existing timer if any
            _scrollTimer?.Stop();

            // Wait for scroll animation to complete (400ms from XAML + buffer)
            _scrollTimer = new System.Timers.Timer(500);
            _scrollTimer.AutoReset = false;
            _scrollTimer.Elapsed += (s, args) =>
            {
                // Update on UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateTitlePosition();
                }, DispatcherPriority.Loaded);
            };
            _scrollTimer.Start();
        }
    }

    private void UpdateTitlePosition()
    {
        if (TitlePanel == null || CurrentSelectedGame == null)
            return;

        try
        {
            var titleY = CalculateTitleY();
            Canvas.SetTop(TitlePanel, titleY);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating title position: {ex.Message}");
        }
    }

    private double CalculateTitleY()
    {
        // Calculate where the title should be positioned vertically
        // Base card size
        const double baseCardHeight = 330;

        // Get the adaptive zoom from converter (same calculation as in converter)
        double windowWidth = Bounds.Width;
        double windowHeight = Bounds.Height;
        double adaptiveZoom = CalculateAdaptiveZoomLocal(windowWidth, windowHeight);

        // Calculate scaled card height
        double scaledCardHeight = baseCardHeight * adaptiveZoom;

        // Selected card is vertically centered in the window
        double cardTop = (Bounds.Height / 2) - (scaledCardHeight / 2);
        double cardBottom = cardTop + scaledCardHeight;

        // Title goes 20px below the card
        const double gapBelowCard = 20;
        return cardBottom + gapBelowCard;
    }

    // Local copy of zoom calculation (same as in CarouselTransformConverter)
    private double CalculateAdaptiveZoomLocal(double windowWidth, double windowHeight)
    {
        double widthFactor = Math.Clamp(windowWidth / 1920.0, 0.75, 1.5);
        double heightFactor = Math.Clamp(windowHeight / 1080.0, 0.8, 1.8);
        double combinedBase = (widthFactor + heightFactor) / 2.0;

        double zoomMultiplier;
        if (windowHeight < 700)
            zoomMultiplier = 1.3;
        else if (windowHeight < 900)
            zoomMultiplier = 1.5;
        else if (windowHeight < 1200)
            zoomMultiplier = 1.7;
        else
            zoomMultiplier = 2.0;

        return zoomMultiplier * combinedBase;
    }

    /// <summary>
    /// True crossfade between two overlapping background images
    /// </summary>
    private async void OnBackgroundImageChanged()
    {
        if (_isAnimating) return;

        var newImagePath = CurrentSelectedGame?.ImagePath;

        // Only animate if the image path actually changed
        if (newImagePath != _lastImagePath && _backgroundImage1 != null && _backgroundImage2 != null)
        {
            _isAnimating = true;
            _lastImagePath = newImagePath;

            try
            {
                // Determine which image to update and which to fade
                var targetImage = _isImage1Active ? _backgroundImage2 : _backgroundImage1;
                var currentImage = _isImage1Active ? _backgroundImage1 : _backgroundImage2;

                // Load new image into the hidden layer
                targetImage.Source = _imageConverter.Convert(newImagePath, typeof(Bitmap), null, null) as Bitmap;

                // Create fade animations
                var fadeOutAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(300),
                    Easing = new CubicEaseInOut(),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0.0),
                            Setters = { new Setter(OpacityProperty, 1.0) }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1.0),
                            Setters = { new Setter(OpacityProperty, 0.0) }
                        }
                    }
                };

                var fadeInAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(300),
                    Easing = new CubicEaseInOut(),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0.0),
                            Setters = { new Setter(OpacityProperty, 0.0) }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1.0),
                            Setters = { new Setter(OpacityProperty, 1.0) }
                        }
                    }
                };

                // Run both animations simultaneously for true crossfade
                await Task.WhenAll(
                    fadeOutAnimation.RunAsync(currentImage),
                    fadeInAnimation.RunAsync(targetImage)
                );

                // Ensure final opacity values are set
                currentImage.Opacity = 0.0;
                targetImage.Opacity = 1.0;

                // Flip active image
                _isImage1Active = !_isImage1Active;
            }
            finally
            {
                _isAnimating = false;
            }
        }
    }
}