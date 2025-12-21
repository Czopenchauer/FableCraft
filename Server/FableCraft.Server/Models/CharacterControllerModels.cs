using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Server.Models;

public record CharacterDto(
    Guid CharacterId,
    string Name,
    string Description,
    CharacterStats CharacterState,
    CharacterTracker? CharacterTracker,
    List<CharacterMemoryDto> CharacterMemories,
    List<CharacterRelationshipDto> Relationships);

public record CharacterMemoryDto(
    string MemoryContent,
    StoryTracker StoryTracker,
    double Salience,
    IDictionary<string, object>? Data);

public record CharacterRelationshipDto(
    string TargetCharacterName,
    IDictionary<string, object> Data,
    int SequenceNumber,
    StoryTracker? StoryTracker);

public record EmulateMainCharacterRequest(string Instruction);

public record KnowledgeGraphSearchRequest(
    Guid? CharacterId,
    bool IsMainCharacter,
    string Query);

public record KnowledgeGraphSearchResponse(List<KnowledgeGraphSearchResultItem> Results);

public record KnowledgeGraphSearchResultItem(string Text);