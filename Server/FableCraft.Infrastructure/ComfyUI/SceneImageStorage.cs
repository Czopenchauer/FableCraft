using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FableCraft.Infrastructure.ComfyUI;

/// <summary>
/// Handles storage and retrieval of generated scene images.
/// Storage structure: {basePath}/{adventureId}/{sceneId}/{version}.png
/// </summary>
public sealed class SceneImageStorage
{
    private readonly ComfyUISettings _settings;
    private readonly ILogger<SceneImageStorage> _logger;

    public SceneImageStorage(
        IOptions<ComfyUISettings> settings,
        ILogger<SceneImageStorage> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Saves an image to storage.
    /// </summary>
    /// <param name="adventureId">The adventure ID.</param>
    /// <param name="sceneId">The scene ID.</param>
    /// <param name="version">The image version number.</param>
    /// <param name="imageBytes">The image data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The relative path where the image was saved.</returns>
    public async Task<string> SaveImageAsync(
        Guid adventureId,
        Guid sceneId,
        int version,
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        var relativePath = GetRelativePath(adventureId, sceneId, version);
        var fullPath = GetFullPath(relativePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogDebug("Created directory: {Directory}", directory);
        }

        await File.WriteAllBytesAsync(fullPath, imageBytes, cancellationToken);
        _logger.LogInformation("Saved scene image: {Path}", relativePath);

        return relativePath;
    }

    /// <summary>
    /// Retrieves an image from storage.
    /// </summary>
    /// <param name="relativePath">The relative path of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The image bytes, or null if not found.</returns>
    public async Task<byte[]?> GetImageAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = GetFullPath(relativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Image not found: {Path}", fullPath);
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    /// <summary>
    /// Deletes an image from storage.
    /// </summary>
    /// <param name="relativePath">The relative path of the image.</param>
    /// <returns>True if the image was deleted, false if it didn't exist.</returns>
    public bool DeleteImage(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);

        if (!File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        _logger.LogInformation("Deleted scene image: {Path}", relativePath);

        // Clean up empty directories
        CleanupEmptyDirectories(Path.GetDirectoryName(fullPath));

        return true;
    }

    /// <summary>
    /// Deletes all images for a scene.
    /// </summary>
    /// <param name="adventureId">The adventure ID.</param>
    /// <param name="sceneId">The scene ID.</param>
    /// <returns>The number of images deleted.</returns>
    public int DeleteAllImagesForScene(Guid adventureId, Guid sceneId)
    {
        var sceneDirectory = GetSceneDirectory(adventureId, sceneId);

        if (!Directory.Exists(sceneDirectory))
        {
            return 0;
        }

        var files = Directory.GetFiles(sceneDirectory, "*.png");
        foreach (var file in files)
        {
            File.Delete(file);
        }

        _logger.LogInformation("Deleted {Count} images for scene {SceneId}", files.Length, sceneId);

        // Clean up empty directories
        CleanupEmptyDirectories(sceneDirectory);

        return files.Length;
    }

    /// <summary>
    /// Gets the next available version number for a scene.
    /// </summary>
    /// <param name="adventureId">The adventure ID.</param>
    /// <param name="sceneId">The scene ID.</param>
    /// <returns>The next version number (1-based).</returns>
    public int GetNextVersion(Guid adventureId, Guid sceneId)
    {
        var sceneDirectory = GetSceneDirectory(adventureId, sceneId);

        if (!Directory.Exists(sceneDirectory))
        {
            return 1;
        }

        var files = Directory.GetFiles(sceneDirectory, "*.png");
        if (files.Length == 0)
        {
            return 1;
        }

        var maxVersion = files
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Select(name => int.TryParse(name, out var v) ? v : 0)
            .Max();

        return maxVersion + 1;
    }

    /// <summary>
    /// Checks if an image exists at the given path.
    /// </summary>
    public bool ImageExists(string relativePath)
    {
        var fullPath = GetFullPath(relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Gets the relative URL path for serving the image.
    /// </summary>
    public static string GetImageUrl(string relativePath)
    {
        // Convert backslashes to forward slashes for URLs
        return $"/scene-images/{relativePath.Replace('\\', '/')}";
    }

    private string GetRelativePath(Guid adventureId, Guid sceneId, int version)
    {
        return Path.Combine(adventureId.ToString(), sceneId.ToString(), $"{version}.png");
    }

    private string GetSceneDirectory(Guid adventureId, Guid sceneId)
    {
        var basePath = GetEffectiveBasePath();
        return Path.Combine(basePath, adventureId.ToString(), sceneId.ToString());
    }

    private string GetFullPath(string relativePath)
    {
        var basePath = GetEffectiveBasePath();
        return Path.Combine(basePath, relativePath);
    }

    private string GetEffectiveBasePath()
    {
        var path = _settings.GetEffectiveImageStoragePath();
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path);
        }
        return path;
    }

    private void CleanupEmptyDirectories(string? directory)
    {
        if (string.IsNullOrEmpty(directory)) return;

        try
        {
            var basePath = GetEffectiveBasePath();

            while (!string.IsNullOrEmpty(directory) &&
                   directory.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) &&
                   directory != basePath)
            {
                if (Directory.Exists(directory) &&
                    !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    _logger.LogDebug("Removed empty directory: {Directory}", directory);
                }
                else
                {
                    break;
                }

                directory = Path.GetDirectoryName(directory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup empty directories");
        }
    }
}
