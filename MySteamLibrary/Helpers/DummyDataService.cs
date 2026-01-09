using System.Collections.ObjectModel;
using MySteamLibrary.Models;

namespace MySteamLibrary.Helpers;

public static class DummyDataService
{
    /// <summary>
    /// Generates a hardcoded list of games for testing the UI layouts.
    /// Returns an ObservableCollection so the UI can react to any changes.
    /// </summary>
    public static ObservableCollection<GameModel> GetFakeGames()
    {
        return new ObservableCollection<GameModel>
        {
            new GameModel
            {
                Title = "Cyberpunk 2077",
                PlayTime = "120 hours",
                Description = "An open-world, action-adventure story set in the megalopolis of Night City.",
                AppId = 1091500
            },
            new GameModel
            {
                Title = "Elden Ring",
                PlayTime = "85 hours",
                Description = "Rise, Tarnished, and be guided by grace to brandish the power of the Elden Ring.",
                AppId = 1245620
            },
            new GameModel
            {
                Title = "Half-Life: Alyx",
                PlayTime = "15 hours",
                Description = "Valve’s VR return to the Half-Life series. It’s the story of an impossible fight against an alien race.",
                AppId = 546560
            },
            new GameModel
            {
                Title = "The Witcher 3: Wild Hunt, this is just a test for a long title",
                PlayTime = "200 hours",
                Description = "A story-driven open world RPG set in a visually stunning fantasy universe.",
                AppId = 292030
            },
            new GameModel
            {
                Title = "Portal 2",
                PlayTime = "12 hours",
                Description = "The sequel to the high-award-winning Portal, featuring a massive single-player campaign.",
                AppId = 6209
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171690
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 17140
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 140
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 6740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 16740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 716740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 1716
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 17167
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171674
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 176740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171740
            },
             new GameModel
            {
                Title = "Cyberpunk 2077",
                PlayTime = "120 hours",
                Description = "An open-world, action-adventure story set in the megalopolis of Night City.",
                AppId = 1091500
            },
            new GameModel
            {
                Title = "Elden Ring",
                PlayTime = "85 hours",
                Description = "Rise, Tarnished, and be guided by grace to brandish the power of the Elden Ring.",
                AppId = 1245620
            },
            new GameModel
            {
                Title = "Half-Life: Alyx",
                PlayTime = "15 hours",
                Description = "Valve’s VR return to the Half-Life series. It’s the story of an impossible fight against an alien race.",
                AppId = 546560
            },
            new GameModel
            {
                Title = "The Witcher 3: Wild Hunt",
                PlayTime = "200 hours",
                Description = "A story-driven open world RPG set in a visually stunning fantasy universe.",
                AppId = 292030
            },
            new GameModel
            {
                Title = "Portal 2",
                PlayTime = "12 hours",
                Description = "The sequel to the high-award-winning Portal, featuring a massive single-player campaign.",
                AppId = 6209
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171690
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 17140
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 140
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 6740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 16740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 716740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 1716
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 17167
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171674
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 176740
            },
            new GameModel
            {
                Title = "Starfield",
                PlayTime = "45 hours",
                Description = "In this next generation role-playing game set amongst the stars, create any character you want.",
                AppId = 171740
            }
        };
    }
}