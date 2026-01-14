using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MySteamLibrary.Models;

namespace MySteamLibrary.Services
{
    /// <summary>
    /// Handles local storage of game metadata and images to reduce API calls.
    /// Now stores cache in a more user-friendly location.
    /// </summary>
    public class CacheService
    {
        // Use AppData/Local for cache - this is the Windows standard location
        private readonly string _cacheFolder;
        private readonly string _metadataFile;
        private readonly HttpClient _httpClient;

        // Semaphore to ensure only one thread writes to the JSON file at a time
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public CacheService()
        {
            _httpClient = new HttpClient();

            // OPTION 1: Use AppData/Local (RECOMMENDED - Windows Standard)
            // This is where most apps store their cache data
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _cacheFolder = Path.Combine(appDataPath, "MySteamLibrary", "Cache");

            // OPTION 2: Use Documents folder (MORE VISIBLE TO USER)
            // Uncomment these lines if you want cache in Documents instead
            // string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // _cacheFolder = Path.Combine(documentsPath, "MySteamLibrary", "Cache");

            // OPTION 3: Use executable location (ORIGINAL BEHAVIOR)
            // Uncomment this if you prefer the old behavior
            // _cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache");

            _metadataFile = Path.Combine(_cacheFolder, "library_cache.json");

            // LOG THE ACTUAL PATHS FOR DEBUGGING
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║        CACHE SERVICE INITIALIZED                       ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"📁 Cache Folder: {_cacheFolder}");
            System.Diagnostics.Debug.WriteLine($"📄 Metadata File: {_metadataFile}");
            System.Diagnostics.Debug.WriteLine("");

            // Ensure the cache directory exists on startup
            EnsureCacheDirectoryExists();
        }

        /// <summary>
        /// Ensures the cache directory exists, creating it if necessary.
        /// This is called both on initialization and before any write operation.
        /// </summary>
        private void EnsureCacheDirectoryExists()
        {
            if (!Directory.Exists(_cacheFolder))
            {
                System.Diagnostics.Debug.WriteLine($"✨ Creating cache directory: {_cacheFolder}");
                Directory.CreateDirectory(_cacheFolder);
                System.Diagnostics.Debug.WriteLine($"✅ Cache directory created successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ Cache directory exists");

                // Show existing cache stats
                if (File.Exists(_metadataFile))
                {
                    var fileInfo = new FileInfo(_metadataFile);
                    System.Diagnostics.Debug.WriteLine($"📊 Existing cache file: {fileInfo.Length:N0} bytes");
                    System.Diagnostics.Debug.WriteLine($"📅 Last modified: {fileInfo.LastWriteTime}");
                }

                // Count existing images
                try
                {
                    var imageFiles = Directory.GetFiles(_cacheFolder, "*_cover.jpg");
                    System.Diagnostics.Debug.WriteLine($"🖼️  Cached images: {imageFiles.Length}");
                }
                catch
                {
                    // Ignore errors when counting files
                }
            }
            System.Diagnostics.Debug.WriteLine("");
        }

        /// <summary>
        /// Returns the cache folder path for external diagnostics
        /// </summary>
        public string GetCacheFolder() => _cacheFolder;

        /// <summary>
        /// Returns the metadata file path for external diagnostics
        /// </summary>
        public string GetMetadataFile() => _metadataFile;

        /// <summary>
        /// Opens the cache folder in Windows Explorer
        /// </summary>
        public void OpenCacheFolderInExplorer()
        {
            try
            {
                // Ensure folder exists before trying to open it
                EnsureCacheDirectoryExists();

                if (Directory.Exists(_cacheFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _cacheFolder);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Cache folder doesn't exist: {_cacheFolder}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error opening cache folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the list of GameModels to a local JSON file using a thread-safe lock.
        /// </summary>
        public async Task SaveLibraryCacheAsync(List<GameModel> games)
        {
            System.Diagnostics.Debug.WriteLine($"💾 Saving {games.Count} games to cache...");

            // CRITICAL: Ensure the cache directory exists before saving
            // This handles the case where the folder was deleted after initialization
            EnsureCacheDirectoryExists();

            // Wait for our turn to access the file
            await _fileLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_metadataFile, json);

                var fileInfo = new FileInfo(_metadataFile);
                System.Diagnostics.Debug.WriteLine($"✅ Cache saved successfully!");
                System.Diagnostics.Debug.WriteLine($"   📊 {games.Count} games, {fileInfo.Length:N0} bytes");
                System.Diagnostics.Debug.WriteLine($"   📁 {_metadataFile}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR saving cache: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Always release the lock so other tasks can use the file
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Loads the game list from the local JSON file.
        /// </summary>
        public async Task<List<GameModel>> LoadLibraryCacheAsync()
        {
            System.Diagnostics.Debug.WriteLine($"📂 Loading cache from: {_metadataFile}");

            if (!File.Exists(_metadataFile))
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️  No cache file found (this is normal on first run)");
                return new List<GameModel>();
            }

            // We also use the lock here to ensure we don't read while the file is being written
            await _fileLock.WaitAsync();
            try
            {
                var json = await File.ReadAllTextAsync(_metadataFile);
                var games = JsonSerializer.Deserialize<List<GameModel>>(json) ?? new List<GameModel>();

                System.Diagnostics.Debug.WriteLine($"✅ Loaded {games.Count} games from cache");
                return games;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR loading cache: {ex.Message}");
                return new List<GameModel>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Downloads a game cover image and saves it locally. 
        /// Returns the local file path, or empty string if all downloads fail.
        /// Tries multiple image URLs from Steam's CDN to find available images.
        /// </summary>
        public async Task<string> GetOrDownloadImageAsync(int appId, string remoteUrl)
        {
            string localPath = Path.Combine(_cacheFolder, $"{appId}_cover.jpg");

            // If the image already exists locally, just return the path
            if (File.Exists(localPath))
            {
                return localPath;
            }

            // CRITICAL: Ensure the cache directory exists before downloading
            EnsureCacheDirectoryExists();

            // Try multiple image sources in order of preference
            var imageUrls = new[]
            {
                remoteUrl, // Primary: library_600x900_2x.jpg
                $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/library_600x900.jpg", // Alternative size
                $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_600x900_2x.jpg", // Alternative CDN
                $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg", // Fallback: header image
                $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900_2x.jpg" // Legacy CDN
            };

            foreach (var url in imageUrls)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"⬇️  Trying to download image: AppId {appId} from {url}");

                    // Download the image data from Steam's CDN
                    var imageData = await _httpClient.GetByteArrayAsync(url);

                    // Verify we got actual image data (not an error page)
                    if (imageData.Length > 1000) // Minimum reasonable image size
                    {
                        await File.WriteAllBytesAsync(localPath, imageData);
                        System.Diagnostics.Debug.WriteLine($"✅ Image saved: {imageData.Length:N0} bytes from {url}");
                        return localPath;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️  Downloaded data too small ({imageData.Length} bytes), trying next URL...");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️  HTTP error for AppId {appId}: {httpEx.Message}");
                    // Continue to next URL
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️  Failed to download from {url}: {ex.Message}");
                    // Continue to next URL
                }
            }

            // All download attempts failed
            System.Diagnostics.Debug.WriteLine($"❌ All image download attempts failed for AppId {appId}");
            System.Diagnostics.Debug.WriteLine($"   Game will display with placeholder image");

            // Return empty string so BitmapValueConverter immediately shows placeholder
            // This avoids repeated network requests and improves performance
            return string.Empty;
        }

        /// <summary>
        /// Gets cache statistics for display
        /// </summary>
        public (int imageCount, long totalSize) GetCacheStats()
        {
            try
            {
                if (!Directory.Exists(_cacheFolder))
                    return (0, 0);

                var files = Directory.GetFiles(_cacheFolder);
                int imageCount = 0;
                long totalSize = 0;

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    totalSize += info.Length;

                    if (file.EndsWith("_cover.jpg"))
                        imageCount++;
                }

                return (imageCount, totalSize);
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}