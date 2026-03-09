using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class LorebookEntry : IEntity
{
    [Required]
    public Guid AdventureId { get; init; }

    public Guid? SceneId { get; init; }

    public Scene? Scene { get; init; }

    public Adventure Adventure { get; init; } = null!;

    public string? Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    public int Priority { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    [Required]
    public string Category { get; set; } = null!;

    public ContentType ContentType { get; set; }

    [Key]
    public Guid Id { get; set; }
}