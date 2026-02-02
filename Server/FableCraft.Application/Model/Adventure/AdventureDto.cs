using System.Text.Json.Serialization;

using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using FluentValidation;

namespace FableCraft.Application.Model.Adventure;

public record AdventureAgentLlmPresetDto(
    Guid LlmPresetId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    AgentName AgentName);

public record ExtraLoreEntryDto(
    string Title,
    string Content,
    string Category);

/// <summary>
///     Represents a pre-defined custom character to be created with the adventure.
/// </summary>
public class CustomCharacterDto
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>
    ///     Character importance tier: "arc_important", "significant", or "background".
    /// </summary>
    public required string Importance { get; init; }

    /// <summary>
    ///     The character's profile (motivations, routine, etc.).
    /// </summary>
    public required CharacterStats CharacterStats { get; init; }

    /// <summary>
    ///     The character's tracker state (location, appearance, etc.).
    /// </summary>
    public required CharacterTracker CharacterTracker { get; init; }

    /// <summary>
    ///     Initial relationships this character has with other characters.
    /// </summary>
    public CustomRelationshipDto[] InitialRelationships { get; init; } = [];
}

/// <summary>
///     Represents an initial relationship for a custom character.
/// </summary>
public class CustomRelationshipDto
{
    /// <summary>
    ///     The name of the character this relationship is about.
    /// </summary>
    public required string TargetCharacterName { get; init; }

    /// <summary>
    ///     2-4 sentences describing the emotional reality of the relationship.
    /// </summary>
    public required string Dynamic { get; init; }

    /// <summary>
    ///     Full relationship data (foundation, stance, trust, desire, intimacy, power, unspoken, developing).
    /// </summary>
    public required IDictionary<string, object> Data { get; init; }
}

public class AdventureDto
{
    public required string Name { get; init; } = string.Empty;

    public required string FirstSceneDescription { get; init; } = string.Empty;

    public required string ReferenceTime { get; init; }

    public required MainCharacterDto MainCharacter { get; init; } = null!;

    public Guid? WorldbookId { get; init; }

    public required Guid TrackerDefinitionId { get; init; }

    public required string PromptPath { get; init; }

    public required AdventureAgentLlmPresetDto[] AgentLlmPresets { get; set; }

    public ExtraLoreEntryDto[] ExtraLoreEntries { get; init; } = [];

    public Guid? GraphRagSettingsId { get; init; }

    /// <summary>
    ///     Pre-defined custom characters to be created with the adventure.
    ///     These characters will exist before the first scene is generated.
    /// </summary>
    public CustomCharacterDto[] CustomCharacters { get; init; } = [];
}

public class AdventureDtoValidator : AbstractValidator<AdventureDto>
{
    private readonly static AgentName[] AllAgentNames = Enum.GetValues<AgentName>();
    private readonly static string[] ValidImportanceValues = ["arc_important", "significant", "background"];

    public AdventureDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Adventure name is required")
            .MaximumLength(200).WithMessage("Adventure name must not exceed 200 characters");

        RuleFor(x => x.MainCharacter)
            .NotNull().WithMessage("MainCharacter is required")
            .SetValidator(new CharacterDtoValidator());

        RuleFor(x => x.AgentLlmPresets)
            .NotNull().WithMessage("Agent LLM presets are required")
            .Must(presets => presets != null && presets.Length == AllAgentNames.Length)
            .WithMessage($"All {AllAgentNames.Length} agent presets must be provided")
            .Must(presets => presets != null && AllAgentNames.All(name => presets.Any(p => p.AgentName == name)))
            .WithMessage("All agent types must have a preset configured");

        RuleFor(p => p.PromptPath)
            .NotEmpty().WithMessage("Prompt path is required");

        RuleForEach(x => x.AgentLlmPresets)
            .ChildRules(preset =>
            {
                preset.RuleFor(p => p.AgentName)
                    .IsInEnum().WithMessage("Invalid agent name");

                preset.RuleFor(p => p.LlmPresetId)
                    .NotEmpty().WithMessage("LLM preset ID is required");
            });

        RuleForEach(x => x.CustomCharacters)
            .SetValidator(new CustomCharacterDtoValidator());
    }
}

public class CustomCharacterDtoValidator : AbstractValidator<CustomCharacterDto>
{
    private readonly static string[] ValidImportanceValues = ["arc_important", "significant", "background"];

    public CustomCharacterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Character name is required")
            .MaximumLength(200).WithMessage("Character name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Character description is required")
            .MaximumLength(20000).WithMessage("Character description must not exceed 20000 characters");

        RuleFor(x => x.Importance)
            .NotEmpty().WithMessage("Character importance is required")
            .Must(x => ValidImportanceValues.Contains(x))
            .WithMessage("Importance must be one of: arc_important, significant, background");

        RuleFor(x => x.CharacterStats)
            .NotNull().WithMessage("CharacterStats is required");

        RuleFor(x => x.CharacterTracker)
            .NotNull().WithMessage("CharacterTracker is required");

        RuleForEach(x => x.InitialRelationships)
            .ChildRules(rel =>
            {
                rel.RuleFor(r => r.TargetCharacterName)
                    .NotEmpty().WithMessage("Target character name is required");

                rel.RuleFor(r => r.Dynamic)
                    .NotEmpty().WithMessage("Relationship dynamic is required");

                rel.RuleFor(r => r.Data)
                    .NotNull().WithMessage("Relationship data is required");
            });
    }
}