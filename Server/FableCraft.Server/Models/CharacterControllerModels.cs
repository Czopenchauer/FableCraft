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

public record CharacterListItemDto(
    Guid CharacterId,
    string Name,
    string Importance);

public record CharacterDetailDto(
    Guid CharacterId,
    string Name,
    string Importance,
    string Description,
    CharacterStats CharacterState,
    CharacterTracker? CharacterTracker,
    List<CharacterMemoryDetailDto> CharacterMemories,
    List<CharacterRelationshipDetailDto> Relationships,
    List<CharacterSceneRewriteDto> SceneRewrites,
    int TotalMemoriesCount,
    int TotalSceneRewritesCount);

public record CharacterMemoryDetailDto(
    Guid Id,
    string MemoryContent,
    SceneTracker SceneTracker,
    double Salience,
    IDictionary<string, object>? Data);

public record CharacterRelationshipDetailDto(
    Guid Id,
    string TargetCharacterName,
    string? Dynamic,
    IDictionary<string, object> Data,
    int SequenceNumber,
    string? UpdateTime);

public record CharacterSceneRewriteDto(
    Guid Id,
    string Content,
    int SequenceNumber,
    SceneTracker SceneTracker);

public record PaginatedResponse<T>(
    List<T> Items,
    int TotalCount,
    int Offset);

public record UpdateCharacterImportanceRequest(string Importance);

public record UpdateCharacterProfileRequest(string Description, CharacterStats CharacterStats);

public record UpdateCharacterTrackerRequest(CharacterTracker Tracker);

public record UpdateCharacterMemoryRequest(
    string Summary,
    double Salience,
    IDictionary<string, object>? Data);

public record UpdateCharacterRelationshipRequest(
    string? Dynamic,
    IDictionary<string, object> Data);

public record CharacterMemoryDto(
    string MemoryContent,
    SceneTracker SceneTracker,
    double Salience,
    IDictionary<string, object>? Data);

public record CharacterRelationshipDto(
    string TargetCharacterName,
    IDictionary<string, object> Data,
    int SequenceNumber,
    string? UpdateTime);

public record EmulateMainCharacterRequest(string Instruction);

public record KnowledgeGraphSearchRequest(
    Guid? CharacterId,
    bool IsMainCharacter,
    string Query);

public record KnowledgeGraphSearchResponse(List<KnowledgeGraphSearchResultItem> Results);

public record KnowledgeGraphSearchResultItem(string Text);

public record RagChatRequest(string Query, string DatasetType);

public record RagChatResponse(string Answer, List<RagChatSource> Sources);

public record RagChatSource(string DatasetName, string Text);