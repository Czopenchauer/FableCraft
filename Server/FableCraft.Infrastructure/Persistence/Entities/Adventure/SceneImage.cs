using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

/// <summary>
/// Status of an image generation request.
/// </summary>
public enum ImageGenerationStatus
{
    /// <summary>
    /// Image generation has been requested but not started.
    /// </summary>
    Pending,

    /// <summary>
    /// Image is currently being generated.
    /// </summary>
    Generating,

    /// <summary>
    /// Image generation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Image generation failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents a generated image for a scene.
/// Supports multiple versions per scene for regeneration.
/// </summary>
public class SceneImage : IEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The scene this image belongs to.
    /// </summary>
    public Guid SceneId { get; init; }

    /// <summary>
    /// Navigation property to the parent scene.
    /// </summary>
    public Scene? Scene { get; init; }

    /// <summary>
    /// Version number for this image (1-based, increments with each regeneration).
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Whether this is the currently selected/displayed version.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Relative path to the stored image file.
    /// Format: {adventureId}/{sceneId}/{version}.png
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// The positive prompt used to generate this image.
    /// </summary>
    [Required]
    public required string Prompt { get; set; }

    /// <summary>
    /// The negative prompt used to generate this image.
    /// </summary>
    public string? NegativePrompt { get; set; }

    /// <summary>
    /// Current status of the image generation.
    /// </summary>
    public ImageGenerationStatus Status { get; set; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this image generation was initiated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// How long the image generation took in milliseconds.
    /// </summary>
    public long GenerationDurationMs { get; set; }
}
