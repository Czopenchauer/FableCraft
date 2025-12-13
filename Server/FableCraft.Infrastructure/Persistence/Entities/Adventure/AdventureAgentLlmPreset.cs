using System.ComponentModel.DataAnnotations;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class AdventureAgentLlmPreset : IEntity
{
    public Guid AdventureId { get; set; }

    public Adventure Adventure { get; set; } = null!;

    public Guid LlmPresetId { get; set; }

    public LlmPreset LlmPreset { get; set; } = null!;

    [Required]
    public required AgentName AgentName { get; set; }

    [Key]
    public Guid Id { get; set; }
}