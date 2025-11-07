using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class CharacterAction : IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SceneId { get; init; }

    public Scene Scene { get; init; }

    [Required]
    public required string ActionDescription { get; init; }

    public bool Selected { get; set; }
}