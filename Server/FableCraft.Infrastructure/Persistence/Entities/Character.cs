using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class Character : IKnowledgeGraphEntity
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    public required string Description { get; init; }

    public required string Background { get; init; }

    [Column(TypeName = "jsonb")]
    public string? StatsJson { get; init; } = null!;

    [Key]
    public Guid Id { get; set; }

    public string GetContent()
    {
        return $"Main Character, {Name} Description: {Description}\n"
               + (string.IsNullOrEmpty(Background) ? string.Empty : $"Main Character Background: {Background}");
    }

    public string GetContentDescription()
    {
        return $"Main Character, {Name} Description";
    }
}