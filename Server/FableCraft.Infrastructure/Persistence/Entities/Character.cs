using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Character : IEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    public required List<CharacterState> CharacterStates { get; set; }
}