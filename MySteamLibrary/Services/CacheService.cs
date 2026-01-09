using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MySteamLibrary.Models;

namespace MySteamLibrary.Services
{
    /// <summary>
    /// Handles local storage of game metadata and images to reduce API calls.
    /// </summary>
    public class CacheService
    {
        // Define paths for the cache folder and the metadata file
        private readonly string _cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");
        private readonly string _metadataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "library_cache.json");
        private readonly HttpClient _httpClient;

        public CacheService()
        {
            _httpClient = new HttpClient();

            // Ensure the cache directory exists on startup
            if (!Directory.Exists(_cacheFolder))
            {
                Directory.CreateDirectory(_cacheFolder);
            }
        }

        /// <summary>
        /// Saves the list of GameModels to a local JSON file.
        /// </summary>
        public async Task SaveLibraryCacheAsync(List<GameModel> games)
        {
            try
            {
                var json = JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_metadataFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the game list from the local JSON file.
        /// </summary>
        public async Task<List<GameModel>> LoadLibraryCacheAsync()
        {
            if (!File.Exists(_metadataFile)) return new List<GameModel>();

            try
            {
                var json = await File.ReadAllTextAsync(_metadataFile);
                return JsonSerializer.Deserialize<List<GameModel>>(json) ?? new List<GameModel>();
            }
            catch
            {
                return new List<GameModel>();
            }
        }

        /// <summary>
        /// Downloads a game cover image and saves it locally. 
        /// Returns the local file path.
        /// </summary>
        public async Task<string> GetOrDownloadImageAsync(int appId, string remoteUrl)
        {
            string localPath = Path.Combine(_cacheFolder, $"{appId}_cover.jpg");

            // If the image already exists locally, just return the path
            if (File.Exists(localPath))
            {
                return localPath;
            }

            try
            {
                // Download the image data from Steam's CDN
                var imageData = await _httpClient.GetByteArrayAsync(remoteUrl);
                await File.WriteAllBytesAsync(localPath, imageData);
                return localPath;
            }
            catch
            {
                // Return the remote URL as a fallback if download fails
                return remoteUrl;
            }
        }
    }
}