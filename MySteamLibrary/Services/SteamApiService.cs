using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using MySteamLibrary.Models;
using MySteamLibrary.ViewModels;

namespace MySteamLibrary.Services
{
    /// <summary>
    /// Service responsible for communicating with the Steam Web API and managing data flow.
    /// </summary>
    public class SteamApiService
    {
        private readonly HttpClient _httpClient;
        private readonly CacheService _cacheService;
        private readonly SettingsViewModel _settings;

        public SteamApiService(SettingsViewModel settings)
        {
            // Initialize the HTTP client for web requests
            _httpClient = new HttpClient();
            // Initialize the cache service for local storage
            _cacheService = new CacheService();
            // Store reference to settings for accessing API key and Steam ID
            _settings = settings;
        }

        /// <summary>
        /// Stage 1: Returns the game list from cache (if exists) or API.
        /// Updated: Ensures all descriptions are initialized with "Loading description..."
        /// to support the UI placeholder style.
        /// </summary>
        public async Task<List<GameModel>> GetLibrarySkeletonAsync()
        {
            // 1. Always load the local cache first to get our saved descriptions/images
            var cachedGames = await _cacheService.LoadLibraryCacheAsync() ?? new List<GameModel>();

            // 2. Always fetch the latest data from Steam to check for new games or playtime updates
            var apiGames = await FetchOwnedGamesFromApiAsync();

            // If the API call fails (e.g., no internet), fall back to whatever is in the cache
            if (apiGames == null || !apiGames.Any())
            {
                return cachedGames;
            }

            // Create a dictionary of the cache for fast lookup (searching by AppId)
            var cachedDict = cachedGames.ToDictionary(g => g.AppId, g => g);
            var updatedList = new List<GameModel>();

            // 3. Compare the API results with our Cache
            foreach (var apiGame in apiGames)
            {
                if (cachedDict.TryGetValue(apiGame.AppId, out var existingGame))
                {
                    // The game exists in cache! Update only the playtime.
                    existingGame.PlaytimeMinutes = apiGame.PlaytimeMinutes;

                    // We keep the 'existingGame' because it already has the 
                    // Description and ImagePath saved from before.
                    updatedList.Add(existingGame);
                }
                else
                {
                    // This is a brand new game not in our cache.
                    apiGame.Description = "Loading description...";
                    updatedList.Add(apiGame);
                }
            }

            // 4. Save the merged results back to the disk
            await _cacheService.SaveLibraryCacheAsync(updatedList);

            return updatedList;
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
        /// Now uses credentials from SettingsViewModel instead of hardcoded values.
        /// </summary>
        private async Task<List<GameModel>> FetchOwnedGamesFromApiAsync()
        {
            string apiKey = _settings.GetApiKey();
            string steamId = _settings.GetSteamId();

            // Validate that credentials are provided
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(steamId))
            {
                System.Diagnostics.Debug.WriteLine("Steam API Key or Steam ID is missing. Please configure in Settings.");
                return new List<GameModel>();
            }

            string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=true&format=json&include_played_free_games=1&skip_unvetted_apps=false";

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