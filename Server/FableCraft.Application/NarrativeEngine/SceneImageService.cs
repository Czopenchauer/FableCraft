using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Infrastructure.Images;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Serilog;

namespace FableCraft.Application.NarrativeEngine;

/// <summary>
/// DTO for scene image data.
/// </summary>
public sealed class SceneImageDto
{
    public required Guid Id { get; init; }
    public required Guid SceneId { get; init; }
    public required int Version { get; init; }
    public required bool IsSelected { get; init; }
    public string? ImageUrl { get; init; }
    public required string Prompt { get; init; }
    public string? NegativePrompt { get; init; }
    public required ImageGenerationStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public long GenerationDurationMs { get; init; }
}

/// <summary>
/// Service for managing scene image generation.
/// </summary>
public interface ISceneImageService
{
    /// <summary>
    /// Gets all images for a scene.
    /// </summary>
    Task<List<SceneImageDto>> GetImagesForSceneAsync(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates a new image for a scene.
    /// </summary>
    Task<SceneImageDto> GenerateImageAsync(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Selects a specific image version as the active image.
    /// </summary>
    Task<SceneImageDto> SelectImageAsync(
        Guid adventureId,
        Guid sceneId,
        Guid imageId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a specific image version.
    /// </summary>
    Task DeleteImageAsync(
        Guid adventureId,
        Guid sceneId,
        Guid imageId,
        CancellationToken cancellationToken);
}

internal sealed class SceneImageService : ISceneImageService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageGenerationClient _imageClient;
    private readonly SceneImageStorage _imageStorage;
    private readonly ImagePromptAgent _imagePromptAgent;
    private readonly IOptionsMonitor<ImageGenerationOptions> _optionsMonitor;
    private readonly ILogger _logger;

    public SceneImageService(
        ApplicationDbContext dbContext,
        IImageGenerationClient imageClient,
        SceneImageStorage imageStorage,
        ImagePromptAgent imagePromptAgent,
        IOptionsMonitor<ImageGenerationOptions> optionsMonitor,
        ILogger logger)
    {
        _dbContext = dbContext;
        _imageClient = imageClient;
        _imageStorage = imageStorage;
        _imagePromptAgent = imagePromptAgent;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public async Task<List<SceneImageDto>> GetImagesForSceneAsync(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        var images = await _dbContext.SceneImages
            .Where(x => x.SceneId == sceneId)
            .OrderByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

        return images.Select(MapToDto).ToList();
    }

    public async Task<SceneImageDto> GenerateImageAsync(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        if (!_optionsMonitor.CurrentValue.Enabled)
        {
            throw new InvalidOperationException("Image generation is not enabled");
        }

        // Load scene with adventure data
        var scene = await _dbContext.Scenes
            .Include(s => s.Adventure)
            .ThenInclude(a => a!.MainCharacter)
            .FirstOrDefaultAsync(s => s.Id == sceneId && s.AdventureId == adventureId, cancellationToken);

        if (scene == null)
        {
            throw new InvalidOperationException($"Scene {sceneId} not found for adventure {adventureId}");
        }

        // Generate prompts using the LLM (no DB write yet — failed generations should not persist).
        var promptInput = new ImagePromptInput
        {
            AdventureId = adventureId,
            SceneId = sceneId,
            PromptPath = scene.Adventure!.PromptPath,
            NarrativeText = scene.NarrativeText,
            SceneTracker = scene.Metadata?.Tracker?.Scene,
            MainCharacterName = scene.Adventure.MainCharacter?.Name,
            MainCharacterAppearance = scene.Metadata?.Tracker?.MainCharacter?.MainCharacter
        };

        ImagePromptOutput promptOutput;
        ImageGenerationResult result;
        try
        {
            promptOutput = await _imagePromptAgent.InvokeAsync(promptInput, cancellationToken);

            result = await _imageClient.GenerateImageAsync(
                promptOutput.PositivePrompt,
                promptOutput.NegativePrompt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate image for scene {SceneId}", sceneId);
            throw;
        }

        // Persist only on success.
        var nextVersion = _imageStorage.GetNextVersion(adventureId, sceneId);
        var imagePath = await _imageStorage.SaveImageAsync(
            adventureId,
            sceneId,
            nextVersion,
            result.ImageBytes,
            cancellationToken);

        var hasOtherImages = await _dbContext.SceneImages
            .AnyAsync(x => x.SceneId == sceneId, cancellationToken);

        var sceneImage = new SceneImage
        {
            Id = Guid.NewGuid(),
            SceneId = sceneId,
            Version = nextVersion,
            IsSelected = !hasOtherImages,
            Status = ImageGenerationStatus.Completed,
            Prompt = promptOutput.PositivePrompt,
            NegativePrompt = promptOutput.NegativePrompt,
            ImagePath = imagePath,
            GenerationDurationMs = result.GenerationDurationMs,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.SceneImages.Add(sceneImage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Generated image for scene {SceneId} version {Version} in {Duration}ms",
            sceneId, nextVersion, result.GenerationDurationMs);

        return MapToDto(sceneImage);
    }

    public async Task<SceneImageDto> SelectImageAsync(
        Guid adventureId,
        Guid sceneId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var image = await _dbContext.SceneImages
            .FirstOrDefaultAsync(x => x.Id == imageId && x.SceneId == sceneId, cancellationToken);

        if (image == null)
        {
            throw new InvalidOperationException($"Image {imageId} not found for scene {sceneId}");
        }

        if (image.Status != ImageGenerationStatus.Completed)
        {
            throw new InvalidOperationException("Cannot select an image that is not completed");
        }

        // Deselect all other images for this scene
        var otherImages = await _dbContext.SceneImages
            .Where(x => x.SceneId == sceneId && x.Id != imageId && x.IsSelected)
            .ToListAsync(cancellationToken);

        foreach (var other in otherImages)
        {
            other.IsSelected = false;
        }

        image.IsSelected = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.Information("Selected image {ImageId} version {Version} for scene {SceneId}",
            imageId, image.Version, sceneId);

        return MapToDto(image);
    }

    public async Task DeleteImageAsync(
        Guid adventureId,
        Guid sceneId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var image = await _dbContext.SceneImages
            .FirstOrDefaultAsync(x => x.Id == imageId && x.SceneId == sceneId, cancellationToken);

        if (image == null)
        {
            throw new InvalidOperationException($"Image {imageId} not found for scene {sceneId}");
        }

        // Delete from storage if exists
        if (!string.IsNullOrEmpty(image.ImagePath))
        {
            _imageStorage.DeleteImage(image.ImagePath);
        }

        // If this was selected, select another image
        if (image.IsSelected)
        {
            var nextImage = await _dbContext.SceneImages
                .Where(x => x.SceneId == sceneId &&
                            x.Id != imageId &&
                            x.Status == ImageGenerationStatus.Completed)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextImage != null)
            {
                nextImage.IsSelected = true;
            }
        }

        _dbContext.SceneImages.Remove(image);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.Information("Deleted image {ImageId} version {Version} for scene {SceneId}",
            imageId, image.Version, sceneId);
    }

    private static SceneImageDto MapToDto(SceneImage image)
    {
        return new SceneImageDto
        {
            Id = image.Id,
            SceneId = image.SceneId,
            Version = image.Version,
            IsSelected = image.IsSelected,
            ImageUrl = !string.IsNullOrEmpty(image.ImagePath)
                ? SceneImageStorage.GetImageUrl(image.ImagePath)
                : null,
            Prompt = image.Prompt,
            NegativePrompt = image.NegativePrompt,
            Status = image.Status,
            ErrorMessage = image.ErrorMessage,
            CreatedAt = image.CreatedAt,
            GenerationDurationMs = image.GenerationDurationMs
        };
    }
}
