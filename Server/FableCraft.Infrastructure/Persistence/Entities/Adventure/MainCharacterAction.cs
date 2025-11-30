using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class MainCharacterAction : IEntity
{
    [Required]
    public Guid SceneId { get; init; }

    public Scene Scene { get; init; } = null!;

    [Required]
    public required string ActionDescription { get; init; }

    public bool Selected { get; set; }

    [Key]
    public Guid Id { get; set; }
}