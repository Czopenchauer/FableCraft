namespace FableCraft.Application.Model;

public class GenerateLorebookDto
{
    public LorebookEntryDto[] Lorebooks { get; init; } = [];

    public string Category { get; init; } = string.Empty;

    public string? AdditionalInstruction { get; init; }
}