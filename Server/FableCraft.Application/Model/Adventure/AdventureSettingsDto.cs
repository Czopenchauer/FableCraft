using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using FluentValidation;

namespace FableCraft.Application.Model.Adventure;

public class AdventureSettingsResponseDto
{
    public required Guid AdventureId { get; init; }

    public required string Name { get; init; }

    public required string PromptPath { get; init; }

    public required List<AgentLlmPresetDto> AgentLlmPresets { get; init; }
}

public class AgentLlmPresetDto
{
    public Guid? Id { get; init; }

    public required string AgentName { get; init; }

    public Guid? LlmPresetId { get; init; }

    public string? LlmPresetName { get; init; }
}

public class UpdateAdventureSettingsDto
{
    public required string PromptPath { get; init; }

    public required List<UpdateAgentLlmPresetDto> AgentLlmPresets { get; init; }
}

public class UpdateAgentLlmPresetDto
{
    public required string AgentName { get; init; }

    public Guid? LlmPresetId { get; init; }
}

public class UpdateAdventureSettingsDtoValidator : AbstractValidator<UpdateAdventureSettingsDto>
{
    public UpdateAdventureSettingsDtoValidator()
    {
        RuleFor(x => x.PromptPath)
            .NotEmpty().WithMessage("Prompt path is required");

        RuleFor(x => x.AgentLlmPresets)
            .NotNull().WithMessage("Agent LLM presets list is required");

        RuleForEach(x => x.AgentLlmPresets)
            .ChildRules(preset =>
            {
                preset.RuleFor(p => p.AgentName)
                    .NotEmpty().WithMessage("Agent name is required")
                    .Must(name => Enum.TryParse<AgentName>(name, out _))
                    .WithMessage("Invalid agent name");
            });
    }
}