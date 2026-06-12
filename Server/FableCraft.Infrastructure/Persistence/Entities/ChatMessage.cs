using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities;

public class ChatMessage : IEntity
{
    public Guid ChatSessionId { get; set; }

    public ChatSession ChatSession { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public required string Role { get; init; }

    [Required]
    public required string Content { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    [Key]
    public Guid Id { get; set; }
}