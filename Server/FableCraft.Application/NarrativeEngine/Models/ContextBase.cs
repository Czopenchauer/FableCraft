namespace FableCraft.Application.NarrativeEngine.Models;

internal class ContextBase
{
    public required SearchResult[] ContextBases { get; set; } = [];

    public CharacterContext[] RelevantCharacters { get; set; } = [];
}

internal class SearchResult
{
    public required string Query { get; set; }

    public required string Response { get; set; }
}