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
        /// Orchestrates the data loading: Checks cache first, otherwise fetches from Steam.
        /// </summary>
        public async Task<List<GameModel>> GetFullLibraryAsync()
        {
            // 1. Try to load existing data from the local JSON cache
            var cachedGames = await _cacheService.LoadLibraryCacheAsync();

            if (cachedGames != null && cachedGames.Any())
            {
                return cachedGames;
            }

            // 2. If no cache exists, fetch the list from Steam Web API
            var games = await FetchOwnedGamesFromApiAsync();

            // 3. For each game found, ensure the image is downloaded locally
            foreach (var game in games)
            {
                string remoteUrl = $"https://cdn.akamai.steamstatic.com/steam/apps/{game.AppId}/library_600x900_2x.jpg";
                game.ImagePath = await _cacheService.GetOrDownloadImageAsync(game.AppId, remoteUrl);
            }

            // 4. Save the processed list (with local paths) to the cache file
            await _cacheService.SaveLibraryCacheAsync(games);

            return games;
        }

        /// <summary>
        /// Loops through the library and fetches descriptions for games that are missing them.
        /// Includes a delay to respect Steam's Store API rate limits.
        /// </summary>
        public async Task RefreshDescriptionsAsync(IEnumerable<GameModel> games)
        {
            var gameList = games.ToList();
            foreach (var game in gameList)
            {
                // Only fetch if we don't have a description yet
                if (string.IsNullOrEmpty(game.Description))
                {
                    game.Description = await GetGameDescriptionAsync(game.AppId);

                    // Delaying for 1.5 seconds to avoid IP block from Steam Store API
                    await Task.Delay(1500);
                }
            }

            // Update the local cache file with the new descriptions
            await _cacheService.SaveLibraryCacheAsync(gameList);
        }

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
                    return StripHtmlTags(htmlDescription);
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