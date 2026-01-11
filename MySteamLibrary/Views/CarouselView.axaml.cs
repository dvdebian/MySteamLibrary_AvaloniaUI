using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MySteamLibrary.ViewModels;
using MySteamLibrary.Models;
using System;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;

namespace MySteamLibrary.Views;

public partial class CarouselView : UserControl
{
    private MainViewModel? _mainVm;
    private IDisposable? _boundsObserver;

    public CarouselView()
    {
        InitializeComponent();

        this.DataContextChanged += (s, e) => TryFindMainViewModel();

        AttachedToVisualTree += (s, e) =>
        {
            TryFindMainViewModel();

            // Use PropertyChanged on the ScrollViewer to watch Bounds
            // This avoids the 'IObserver' lambda conversion error
            PART_ScrollViewer.PropertyChanged += ScrollViewer_PropertyChanged;

            Dispatcher.UIThread.Post(() => this.Focus(), DispatcherPriority.Background);
        };

        DetachedFromVisualTree += (s, e) =>
        {
            if (PART_ScrollViewer != null)
                PART_ScrollViewer.PropertyChanged -= ScrollViewer_PropertyChanged;
        };
    }

    private void TryFindMainViewModel()
    {
        if (DataContext is CoverViewModel coverVm && coverVm.Parent is MainViewModel vm1)
            SetMainViewModel(vm1, "Direct Parent");
        else if (DataContext is MainViewModel vm2)
            SetMainViewModel(vm2, "Direct DataContext");
        else if (VisualRoot is Window window && window.DataContext is MainViewModel vm3)
            SetMainViewModel(vm3, "Window DataContext");
        else
            Debug.WriteLine($"[Carousel] DataContext is {DataContext?.GetType().Name ?? "NULL"}. Still searching...");
    }

    private void SetMainViewModel(MainViewModel vm, string source)
    {
        if (_mainVm == vm) return;
        if (_mainVm != null) _mainVm.PropertyChanged -= OnViewModelPropertyChanged;

        _mainVm = vm;
        _mainVm.PropertyChanged += OnViewModelPropertyChanged;

        Debug.WriteLine($"[Carousel] SUCCESS: Linked to MainViewModel via {source}");
        ScrollToSelected();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // React to the selection changing in the MainViewModel
        if (e.PropertyName == "SelectedGame")
        {
            ScrollToSelected();
        }
    }

    private void ScrollToSelected()
    {
        // DispatcherPriority.Loaded is crucial: it waits for the layout pass 
        // to finish so the ScrollViewer knows its new actual width.
        Dispatcher.UIThread.Post(() =>
        {
            var mainVm = _mainVm ?? (VisualRoot as Window)?.DataContext as MainViewModel;
            var carouselVm = CarouselRepeater?.DataContext as CarouselViewModel;

            if (mainVm?.SelectedGame == null || carouselVm == null || PART_ScrollViewer == null)
                return;

            var gamesList = carouselVm.Games.ToList();
            int index = gamesList.FindIndex(g => g.Title == mainVm.SelectedGame.Title);

            if (index == -1) return;

            // --- SLOT-BASED MATH ---
            // Card Width (220) + Spacing (10) = 230
            const double itemFullWidth = 230;

            // Because the CenterPaddingConverter handles centering at Offset 0,
            // we just shift by the index multiplied by the slot width.
            double targetX = index * itemFullWidth;

            // Apply the offset. The VectorTransition in AXAML handles the slide animation.
            PART_ScrollViewer.Offset = new Vector(targetX, 0);

            Debug.WriteLine($"[Carousel] Re-centering: {mainVm.SelectedGame.Title} at Index {index}, Offset {targetX}");
        }, DispatcherPriority.Loaded);
    }

    private void OnCoverClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is GameModel clickedGame && _mainVm != null)
        {
            if (_mainVm.SelectedGame?.Title == clickedGame.Title)
                _mainVm.OpenGameDetailsCommand.Execute(clickedGame);
            else
                _mainVm.SelectedGame = clickedGame;

            this.Focus();
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var mainVm = _mainVm ?? (VisualRoot as Window)?.DataContext as MainViewModel;
        var carouselVm = CarouselRepeater?.DataContext as CarouselViewModel;

        if (mainVm == null || carouselVm == null) return;

        var gamesList = carouselVm.Games.ToList();
        if (gamesList.Count == 0) return;

        string currentTitle = mainVm.SelectedGame?.Title ?? "";
        int currentIndex = gamesList.FindIndex(g => g.Title == currentTitle);

        if (currentIndex == -1) currentIndex = 0;

        if (e.Delta.Y < 0 && currentIndex < gamesList.Count - 1)
        {
            mainVm.SelectedGame = gamesList[currentIndex + 1];
        }
        else if (e.Delta.Y > 0 && currentIndex > 0)
        {
            mainVm.SelectedGame = gamesList[currentIndex - 1];
        }

        e.Handled = true;
        this.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var mainVm = _mainVm ?? (VisualRoot as Window)?.DataContext as MainViewModel;
        var carouselVm = CarouselRepeater?.DataContext as CarouselViewModel;

        if (mainVm == null || carouselVm == null) return;

        var gamesList = carouselVm.Games.ToList();
        if (gamesList.Count == 0) return;

        string currentTitle = mainVm.SelectedGame?.Title ?? "";
        int currentIndex = gamesList.FindIndex(g => g.Title == currentTitle);

        if (currentIndex == -1) currentIndex = 0;

        bool handled = false;
        if (e.Key == Key.Right && currentIndex < gamesList.Count - 1)
        {
            mainVm.SelectedGame = gamesList[currentIndex + 1];
            handled = true;
        }
        else if (e.Key == Key.Left && currentIndex > 0)
        {
            mainVm.SelectedGame = gamesList[currentIndex - 1];
            handled = true;
        }

        if (handled)
        {
            e.Handled = true;
            this.Focus();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ScrollToSelected();
    }

    private void ScrollViewer_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Control.BoundsProperty)
        {
            ScrollToSelected();
        }
    }
}