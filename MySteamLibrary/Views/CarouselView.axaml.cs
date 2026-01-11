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

    public CarouselView()
    {
        InitializeComponent();
        this.DataContextChanged += (s, e) => TryFindMainViewModel();
        AttachedToVisualTree += (s, e) =>
        {
            TryFindMainViewModel();
            Dispatcher.UIThread.Post(() => this.Focus(), DispatcherPriority.Background);
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
        if (e.PropertyName == "SelectedGame")
        {
            Debug.WriteLine($"[Carousel] PropertyChanged detected: SelectedGame is now {_mainVm?.SelectedGame?.Title ?? "NULL"}");
            ScrollToSelected();
        }
    }

    private void ScrollToSelected()
    {
        var mainVm = (VisualRoot as Window)?.DataContext as MainViewModel;
        var carouselVm = CarouselRepeater?.DataContext as CarouselViewModel;

        if (mainVm?.SelectedGame == null || carouselVm == null || PART_ScrollViewer == null)
            return;

        var gamesList = carouselVm.Games.ToList();
        int index = gamesList.FindIndex(g => g.Title == mainVm.SelectedGame.Title);

        if (index == -1) return;

        // --- COORDINATED MATH ---
        // These must match your AXAML Spacing (10) and Card Width (220)
        const double itemWidth = 220;
        const double spacing = 10;
        const double itemFullWidth = itemWidth + spacing;

        // The target offset is simply the index multiplied by the full width of one slot.
        // Because our Converter (above) already centered the first item at 0,
        // moving by exactly 'itemFullWidth' will center the next one perfectly.
        double targetX = index * itemFullWidth;

        // Apply the offset
        // The VectorTransition in AXAML handles the smooth sliding animation
        PART_ScrollViewer.Offset = new Vector(targetX, 0);

        Debug.WriteLine($"[Carousel] Snapping to {mainVm.SelectedGame.Title} at index {index}. Offset: {targetX}");
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

    // UPDATED: Find current index by Title to ensure the selection changes
    // Updated: Uses FindIndex with Title to ensure selection actually changes
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        Debug.WriteLine($"[Carousel] PointerWheel detected. Delta: {e.Delta.Y}");

        // 1. Get MainVM (Global Selection State)
        var mainVm = _mainVm ?? (VisualRoot as Window)?.DataContext as MainViewModel;

        // 2. Get the list of games from CarouselViewModel (Verified by your logs!)
        var carouselVm = CarouselRepeater?.DataContext as CarouselViewModel;

        if (mainVm == null || carouselVm == null)
        {
            Debug.WriteLine($"[Carousel] Wheel ABORT: MainVM={mainVm != null}, CarouselVM={carouselVm != null}");
            return;
        }

        // 3. Get the list of games
        // Note: Assuming CarouselViewModel has the 'Games' collection
        var gamesList = carouselVm.Games.ToList();
        if (gamesList.Count == 0) return;

        // 4. Find index by Title
        string currentTitle = mainVm.SelectedGame?.Title ?? "";
        int currentIndex = gamesList.FindIndex(g => g.Title == currentTitle);

        if (currentIndex == -1) currentIndex = 0;

        // 5. Change selection
        if (e.Delta.Y < 0 && currentIndex < gamesList.Count - 1)
        {
            mainVm.SelectedGame = gamesList[currentIndex + 1];
            Debug.WriteLine($"[Carousel] Wheel SUCCESS: Next -> {gamesList[currentIndex + 1].Title}");
        }
        else if (e.Delta.Y > 0 && currentIndex > 0)
        {
            mainVm.SelectedGame = gamesList[currentIndex - 1];
            Debug.WriteLine($"[Carousel] Wheel SUCCESS: Prev -> {gamesList[currentIndex - 1].Title}");
        }

        e.Handled = true;
        this.Focus();
    }

    // Updated: Keyboard navigation now uses Title-based search as well
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // 1. Get MainVM via the Window (Source of truth for selection)
        var mainVm = (VisualRoot as Window)?.DataContext as MainViewModel;

        // 2. Get CarouselVM directly from the Repeater's DataContext
        var carouselVm = CarouselRepeater?.DataContext as CarouselViewModel;

        if (mainVm == null || carouselVm == null)
        {
            Debug.WriteLine($"[Carousel] KeyDown ABORT: MainVM={mainVm != null}, CarouselVM={carouselVm != null}");
            return;
        }

        // 3. Get the list of games
        var gamesList = carouselVm.Games.ToList();
        if (gamesList.Count == 0) return;

        // 4. Find current index by Title
        string currentTitle = mainVm.SelectedGame?.Title ?? "";
        int currentIndex = gamesList.FindIndex(g => g.Title == currentTitle);

        // Default to 0 if nothing is selected
        if (currentIndex == -1) currentIndex = 0;

        // 5. Handle Navigation Keys
        bool handled = false;
        if (e.Key == Key.Right && currentIndex < gamesList.Count - 1)
        {
            mainVm.SelectedGame = gamesList[currentIndex + 1];
            Debug.WriteLine($"[Carousel] Key SUCCESS: Right -> {gamesList[currentIndex + 1].Title}");
            handled = true;
        }
        else if (e.Key == Key.Left && currentIndex > 0)
        {
            mainVm.SelectedGame = gamesList[currentIndex - 1];
            Debug.WriteLine($"[Carousel] Key SUCCESS: Left -> {gamesList[currentIndex - 1].Title}");
            handled = true;
        }

        if (handled)
        {
            e.Handled = true;
            // Keep focus on this control so subsequent key presses work
            this.Focus();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ScrollToSelected();
    }
}