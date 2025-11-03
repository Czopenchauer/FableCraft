using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Character : IKnowledgeGraphEntity, IEntity
{
    [Key]
    public Guid Id { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    public required string Description { get; init; }

    public required string Background { get; init; }

    [MaxLength(64)]
    public string? KnowledgeGraphNodeId { get; init; }

    public ProcessingStatus ProcessingStatus { get; set; }

    [Column(TypeName = "jsonb")]
    public string? StatsJson { get; init; } = null!;
}