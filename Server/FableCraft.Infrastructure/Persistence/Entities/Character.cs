using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Character : IKnowledgeGraphEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required CharacterState[] CharacterStates { get; set; }

    public Content GetContent()
    {
        return new Content(Description,
            Name,
            ContentType.Text);
    }
}