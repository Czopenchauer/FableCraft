using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using FluentValidation;

namespace FableCraft.Application.Model;

public class TrackerDefinitionDto
{
    public required string Name { get; init; } = string.Empty;

    public required TrackerStructure Structure { get; init; } = null!;
}

public class TrackerDefinitionResponseDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public required TrackerStructure Structure { get; init; } = null!;
}

public class TrackerDefinitionDtoValidator : AbstractValidator<TrackerDefinitionDto>
{
    public TrackerDefinitionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tracker definition name is required")
            .MaximumLength(200).WithMessage("Tracker definition name must not exceed 200 characters");

        RuleFor(x => x.Structure)
            .NotNull().WithMessage("Tracker structure is required");

        // Validate Story section
        RuleFor(x => x.Structure.Story)
            .NotNull().WithMessage("Story section is required")
            .NotEmpty().WithMessage("Story section must contain at least the framework fields")
            .Must(story => HasRequiredField(story, "Time", FieldType.String))
            .WithMessage("Story section must contain a 'Time' field of type String")
            .Must(story => HasRequiredField(story, "Weather", FieldType.String))
            .WithMessage("Story section must contain a 'Weather' field of type String")
            .Must(story => HasRequiredField(story, "Location", FieldType.String))
            .WithMessage("Story section must contain a 'Location' field of type String");

        // Validate CharactersPresent section
        RuleFor(x => x.Structure.CharactersPresent)
            .NotNull().WithMessage("CharactersPresent field is required")
            .Must(field => field.Type == FieldType.Array)
            .WithMessage("CharactersPresent must be of type Array");

        // Validate MainCharacter section
        RuleFor(x => x.Structure.MainCharacter)
            .NotNull().WithMessage("MainCharacter section is required")
            .NotEmpty().WithMessage("MainCharacter section must contain at least the framework fields")
            .Must(mainChar => HasRequiredField(mainChar, "Name", FieldType.String))
            .WithMessage("MainCharacter section must contain a 'Name' field of type String");

        // Validate Characters section
        RuleFor(x => x.Structure.Characters)
            .NotNull().WithMessage("Characters section is required")
            .NotEmpty().WithMessage("Characters section must contain at least the framework fields")
            .Must(characters => HasRequiredField(characters, "Name", FieldType.String))
            .WithMessage("Characters section must contain a 'Name' field of type String");
    }

    private static bool HasRequiredField(FieldDefinition[] fields, string fieldName, FieldType expectedType)
    {
        return fields.Any(f => f.Name == fieldName && f.Type == expectedType);
    }
}

public static class TrackerDefinitionFactory
{
    public static TrackerStructure CreateDefaultStructure()
    {
        return new TrackerStructure
        {
            Story =
            [
                new FieldDefinition
                {
                    Name = "Time",
                    Type = FieldType.String,
                    Prompt =
                        "Adjust time in small increments for natural progression unless explicit directives indicate larger changes. Format: ISO 8601 (YYYY-MM-DDTHH:MM:SS).",
                    DefaultValue = "2024-10-16T09:15:30",
                    ExampleValues =
                    [
                        "2024-10-16T09:15:30",
                        "2024-10-16T18:45:50",
                        "2024-10-16T15:10:20"
                    ]
                },
                new FieldDefinition
                {
                    Name = "Weather",
                    Type = FieldType.String,
                    Prompt = "Describe current weather concisely to set the scene.",
                    DefaultValue = "Overcast, mild temperature",
                    ExampleValues =
                    [
                        "Overcast, mild temperature",
                        "Clear skies, warm evening",
                        "Sunny, gentle sea breeze"
                    ]
                },
                new FieldDefinition
                {
                    Name = "Location",
                    Type = FieldType.String,
                    Prompt =
                        "Provide a detailed and specific location, including exact places like rooms, landmarks, or stores, following this format: 'Specific Place, Building, City, State'.",
                    DefaultValue = "Conference Room B, 12th Floor, Apex Corporation, New York, NY",
                    ExampleValues =
                    [
                        "Conference Room B, 12th Floor, Apex Corporation, New York, NY",
                        "Main Gym Hall, Maple Street Fitness Center, Denver, CO",
                        "South Beach, Miami, FL"
                    ]
                }
            ],
            CharactersPresent = new FieldDefinition
            {
                Name = "CharactersPresent",
                Type = FieldType.Array,
                Prompt = "List of all characters present in the scene.",
                DefaultValue = new[] { "No Characters" },
                ExampleValues =
                [
                    new[] { "Emma Thompson", "James Miller" }
                ]
            },
            MainCharacter =
            [
                new FieldDefinition
                {
                    Name = "Name",
                    Type = FieldType.String,
                    Prompt = "The character's full name.",
                    DefaultValue = "Ariel",
                    ExampleValues =
                    [
                        "Ariel",
                        "Kael",
                        "Valerius"
                    ]
                }
            ],
            Characters =
            [
                new FieldDefinition
                {
                    Name = "Name",
                    Type = FieldType.String,
                    Prompt = "The character's full name.",
                    DefaultValue = "Ariel",
                    ExampleValues =
                    [
                        "Ariel",
                        "Kael",
                        "Valerius"
                    ]
                }
            ]
        };
    }
}