using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class LorebookEntry : IKnowledgeGraphEntity
{
    [Required]
    public Guid AdventureId { get; init; }

    public Adventure Adventure { get; init; } = null!;

    [Required]
    public string Description { get; init; } = null!;

    public int Priority { get; init; }

    [Required]
    public string Content { get; init; } = null!;

    [Required]
    public string Category { get; init; } = null!;

    [Key]
    public Guid Id { get; set; }

    public string GetContent()
    {
        return Content;
    }

    public string GetContentDescription()
    {
        return Description;
    }
}