// ViewModelBase.cs
using CommunityToolkit.Mvvm.ComponentModel;

public class ViewModelBase : ObservableObject { }

// A specific base for Library Views (Grid, List, etc.)
public class LibraryPresenterViewModel : ViewModelBase { }