using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Character : IEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    public required string Description { get; init; }

    public required string Background { get; init; }

    [Column(TypeName = "jsonb")]
    public string? StatsJson { get; init; } = null!;
}