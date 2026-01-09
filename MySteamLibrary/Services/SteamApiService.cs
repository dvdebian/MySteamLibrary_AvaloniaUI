using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using MySteamLibrary.Models;

namespace MySteamLibrary.Services
{
    /// <summary>
    /// Service responsible for communicating with the Steam Web API and managing data flow.
    /// </summary>
    public class SteamApiService
    {
        private readonly HttpClient _httpClient;
        private readonly CacheService _cacheService;

        // Credentials for the Steam Web API
        private const string ApiKey = "0DDEF6A47FB7E0304DF047955F976F4C";
        private const string SteamId = "76561198117663948";

        public SteamApiService()
        {
            // Initialize the HTTP client for web requests
            _httpClient = new HttpClient();
            // Initialize the cache service for local storage
            _cacheService = new CacheService();
        }

        /// <summary>
        /// Stage 1: Returns the game list from cache (if exists) or API.
        /// Updated: Ensures all descriptions are initialized with "Loading description..."
        /// to support the UI placeholder style.
        /// </summary>
        public async Task<List<GameModel>> GetLibrarySkeletonAsync()
        {
            // 1. Try to load from local cache first
            var cachedGames = await _cacheService.LoadLibraryCacheAsync();

            if (cachedGames != null && cachedGames.Any())
            {
                // Ensure cached games with missing descriptions show the "Loading..." text
                foreach (var game in cachedGames)
                {
                    if (string.IsNullOrWhiteSpace(game.Description))
                    {
                        game.Description = "Loading description...";
                    }
                }
                return cachedGames;
            }

            // 2. Fetch the list from Steam Web API if no cache exists
            var apiGames = await FetchOwnedGamesFromApiAsync();

            // Initialize new API games with the loading state
            foreach (var game in apiGames)
            {
                game.Description = "Loading description...";
            }

            return apiGames;
        }

        /// <summary>
        /// Stage 3: Downloads the image for a specific game and updates its path.
        /// </summary>
        public async Task LoadGameImageAsync(GameModel game)
        {
            string remoteUrl = $"https://cdn.akamai.steamstatic.com/steam/apps/{game.AppId}/library_600x900_2x.jpg";

            // GetOrDownloadImageAsync handles the local check internally
            game.ImagePath = await _cacheService.GetOrDownloadImageAsync(game.AppId, remoteUrl);
        }

        /// <summary>
        /// Stage 4: Loops through the library and fetches descriptions.
        /// Updated: Fetches if current text is the "Loading..." placeholder.
        /// </summary>
        public async Task RefreshDescriptionsAsync(IEnumerable<GameModel> games)
        {
            var gameList = games.ToList();
            int count = 0;

            foreach (var game in gameList)
            {
                // Only fetch if empty or currently showing the placeholder/error state
                if (string.IsNullOrWhiteSpace(game.Description) ||
                    game.Description == "Loading description..." ||
                    game.Description == "No description available.")
                {
                    game.Description = await GetGameDescriptionAsync(game.AppId);
                    count++;

                    // Save cache every 5 games to persist progress without hitting the disk too hard
                    if (count % 5 == 0)
                    {
                        await _fileLockSave(gameList);
                    }

                    // Delaying for 1.5 seconds to avoid IP block from Steam Store API
                    await Task.Delay(1500);
                }
            }

            // Final save to ensure all progress is captured
            await _cacheService.SaveLibraryCacheAsync(gameList);
        }

        // Helper to handle periodic saves within the loop
        private async Task _fileLockSave(List<GameModel> list) => await _cacheService.SaveLibraryCacheAsync(list);

        /// <summary>
        /// Fetches the 'short_description' from the Steam Storefront API.
        /// </summary>
        public async Task<string> GetGameDescriptionAsync(int appId)
        {
            string url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l=english";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                var root = doc.RootElement.GetProperty(appId.ToString());

                if (root.GetProperty("success").GetBoolean())
                {
                    var data = root.GetProperty("data");
                    string htmlDescription = data.GetProperty("short_description").GetString() ?? string.Empty;

                    // Clean the HTML tags before returning the text
                    string cleaned = StripHtmlTags(htmlDescription);
                    return string.IsNullOrWhiteSpace(cleaned) ? "No description available." : cleaned;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching description for {appId}: {ex.Message}");
            }

            return "No description available.";
        }

        /// <summary>
        /// Communicates directly with the Steam Web API to get the owned games list.
        /// </summary>
        private async Task<List<GameModel>> FetchOwnedGamesFromApiAsync()
        {
            string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={ApiKey}&steamid={SteamId}&include_appinfo=true&format=json";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement.GetProperty("response");

                if (!root.TryGetProperty("games", out var gamesJson))
                {
                    return new List<GameModel>();
                }

                var gameList = new List<GameModel>();
                foreach (var game in gamesJson.EnumerateArray())
                {
                    gameList.Add(new GameModel
                    {
                        AppId = game.GetProperty("appid").GetInt32(),
                        Title = game.GetProperty("name").GetString() ?? "Unknown Game",
                        PlaytimeMinutes = game.GetProperty("playtime_forever").GetInt32()
                    });
                }
                return gameList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Steam API Error: {ex.Message}");
                return new List<GameModel>();
            }
        }

        /// <summary>
        /// Simple Regex helper to remove HTML tags like <b>, <br>, etc.
        /// </summary>
        private string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Removes anything between < and >
            return Regex.Replace(input, "<.*?>", String.Empty).Trim();
        }
    }
}