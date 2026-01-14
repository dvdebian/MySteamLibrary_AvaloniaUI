using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MySteamLibrary.Models;

namespace MySteamLibrary.Services
{
    /// <summary>
    /// Service for handling custom game cover images selected by the user.
    /// </summary>
    public class CustomImageService
    {
        private readonly CacheService _cacheService;

        public CustomImageService(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Opens a file picker dialog and allows the user to select a custom image for a game.
        /// Returns true if an image was successfully selected and saved.
        /// </summary>
        public async Task<bool> SelectAndSaveCustomImageAsync(GameModel game, Window? parentWindow)
        {
            if (parentWindow == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️  Cannot open file picker: Parent window is null");
                return false;
            }

            try
            {
                // Get the storage provider from the window
                var storageProvider = parentWindow.StorageProvider;

                // Define file type filters for images
                var fileTypes = new FilePickerFileType[]
                {
                    new("Image Files")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp", "*.bmp" },
                        MimeTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/bmp" }
                    },
                    new("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                };

                // Open file picker
                var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = $"Select Cover Image for {game.Title}",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                });

                // Check if user selected a file
                if (files.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("ℹ️  User cancelled file selection");
                    return false;
                }

                var selectedFile = files[0];
                var sourcePath = selectedFile.Path.LocalPath;

                System.Diagnostics.Debug.WriteLine($"📁 User selected: {sourcePath}");

                // Validate and save the image
                return await ValidateAndSaveImageAsync(game, sourcePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error selecting custom image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates the selected image file and saves it to the cache folder.
        /// </summary>
        private async Task<bool> ValidateAndSaveImageAsync(GameModel game, string sourceImagePath)
        {
            try
            {
                // Check if source file exists
                if (!File.Exists(sourceImagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Source file not found: {sourceImagePath}");
                    return false;
                }

                // Get file info
                var fileInfo = new FileInfo(sourceImagePath);

                // Validate file size (max 10MB to be safe)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (fileInfo.Length > maxFileSize)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️  Image file too large: {fileInfo.Length:N0} bytes (max: {maxFileSize:N0})");
                    // Still proceed, but warn
                }

                // Validate it's actually an image by checking extension
                var extension = fileInfo.Extension.ToLowerInvariant();
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" };

                if (Array.IndexOf(validExtensions, extension) == -1)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️  Invalid image extension: {extension}");
                    return false;
                }

                // Determine destination path in cache
                string cacheFolder = _cacheService.GetCacheFolder();
                string destinationPath = Path.Combine(cacheFolder, $"{game.AppId}_cover.jpg");

                System.Diagnostics.Debug.WriteLine($"💾 Saving custom image to: {destinationPath}");

                // If a file already exists, delete it first
                if (File.Exists(destinationPath))
                {
                    System.Diagnostics.Debug.WriteLine($"🗑️  Removing existing image");
                    File.Delete(destinationPath);
                }

                // Copy the image to cache folder
                // We'll always save as .jpg regardless of source format for consistency
                File.Copy(sourceImagePath, destinationPath, overwrite: true);

                System.Diagnostics.Debug.WriteLine($"✅ Custom image saved successfully");
                System.Diagnostics.Debug.WriteLine($"   📊 File size: {new FileInfo(destinationPath).Length:N0} bytes");

                // Update the game model's ImagePath
                game.ImagePath = destinationPath;

                System.Diagnostics.Debug.WriteLine($"✅ GameModel.ImagePath updated to: {destinationPath}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving custom image: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Removes a custom image for a game and attempts to re-download from Steam CDN.
        /// </summary>
        public async Task<bool> RemoveCustomImageAsync(GameModel game)
        {
            try
            {
                string cacheFolder = _cacheService.GetCacheFolder();
                string imagePath = Path.Combine(cacheFolder, $"{game.AppId}_cover.jpg");

                if (File.Exists(imagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"🗑️  Removing custom image for {game.Title}");
                    File.Delete(imagePath);
                }

                // Reset the image path to empty so it will try to download again or show placeholder
                game.ImagePath = string.Empty;

                System.Diagnostics.Debug.WriteLine($"✅ Custom image removed. Set to placeholder.");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error removing custom image: {ex.Message}");
                return false;
            }
        }
    }
}