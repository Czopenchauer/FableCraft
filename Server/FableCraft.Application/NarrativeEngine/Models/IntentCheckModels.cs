namespace FableCraft.Application.NarrativeEngine.Models;

/// <summary>
///     Input for IntentCheckAgent - character planning their intentions for an upcoming period.
/// </summary>
internal sealed class IntentCheckInput
{
    public required CharacterContext Character { get; init; }

    public required string[] ArcImportantCharacters { get; init; }

    public string? WorldEvents { get; init; }

    public string? PreviousIntentions { get; init; }
}