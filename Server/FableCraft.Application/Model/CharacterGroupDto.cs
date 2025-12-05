using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Application.Model;

public class CharacterGroupDto
{
    public required Guid CharacterId { get; init; }

    public required List<CharacterVersionDto> Versions { get; init; }
}

public class CharacterVersionDto
{
    public required Guid Id { get; init; }

    public required Guid SceneId { get; init; }

    public required int SequenceNumber { get; init; }

    public required string Description { get; init; }

    public required CharacterStats CharacterStats { get; init; }

    public required CharacterTracker Tracker { get; init; }
}