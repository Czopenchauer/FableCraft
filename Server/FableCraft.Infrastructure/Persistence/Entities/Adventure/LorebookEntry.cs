using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class LorebookEntry : IEntity
{
    [Required]
    public Guid AdventureId { get; init; }

    public Guid? SceneId { get; init; }

    public Scene? Scene { get; init; }

    public Adventure Adventure { get; init; } = null!;

    [Required]
    public string Description { get; init; } = null!;

    public int Priority { get; init; }

    [Required]
    public string Content { get; init; } = null!;

    [Required]
    public string Category { get; init; } = null!;

    public ContentType ContentType { get; init; }

    [Key]
    public Guid Id { get; set; }
}