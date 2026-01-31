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
}

public class AdventureDtoValidator : AbstractValidator<AdventureDto>
{
    private readonly static AgentName[] AllAgentNames = Enum.GetValues<AgentName>();

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
    }
}