using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using MySteamLibrary.Converters;
using MySteamLibrary.Models;
using MySteamLibrary.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MySteamLibrary.Views;

/// <summary>
/// Code-behind for CoverView. 
/// Handles magnetic centering, window resize recalculation, and smooth background transitions.
/// </summary>
public partial class CoverView : UserControl
{
    private Image? _backgroundImage1;
    private Image? _backgroundImage2;
    private bool _isImage1Active = true;
    private string? _lastImagePath;
    private bool _isAnimating = false;
    private readonly BitmapValueConverter _imageConverter = new();

    public CoverView()
    {
        InitializeComponent();

        // Trigger centering whenever the selection changes
        CoverList.SelectionChanged += OnSelectionChanged;

        // Listen for keyboard
        CoverList.KeyDown += OnKeyDown;

        // Ensure we center on load and grab focus so Arrow Keys work immediately
        AttachedToVisualTree += (s, e) =>
        {
            ScrollToSelected();
            Dispatcher.UIThread.Post(() => CoverList.Focus(), DispatcherPriority.Background);
        };

        // Setup dual background images for crossfade
        Loaded += (s, e) =>
        {
            _backgroundImage1 = this.FindControl<Image>("BackgroundImage1");
            _backgroundImage2 = this.FindControl<Image>("BackgroundImage2");

            if (DataContext is CoverViewModel viewModel && viewModel.Parent != null)
            {
                // Set initial image
                var initialPath = viewModel.Parent.SelectedGame?.ImagePath;
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

                viewModel.Parent.PropertyChanged += OnSelectedGameChanged;
            }
        };

        Unloaded += (s, e) =>
        {
            if (DataContext is CoverViewModel viewModel && viewModel.Parent != null)
            {
                viewModel.Parent.PropertyChanged -= OnSelectedGameChanged;
            }
        };
    }

    /// <summary>
    /// True crossfade between two overlapping images
    /// </summary>
    private async void OnSelectedGameChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedGame) && !_isAnimating)
        {
            var mainViewModel = sender as MainViewModel;
            var newImagePath = mainViewModel?.SelectedGame?.ImagePath;

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

    private void OnCoverClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is GameModel clickedGame)
        {
            if (CoverList.SelectedItem == clickedGame)
            {
                if (DataContext is CoverViewModel viewModel)
                {
                    viewModel.Parent.OpenGameDetailsCommand.Execute(clickedGame);
                    e.Handled = true;
                }
            }
            CoverList.Focus();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
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
        CoverList.Focus();
    }
}