using System.Text.Json;

namespace FableCraft.Server.Models;

public record UpdateSceneNarrativeRequest(string NarrativeText);

public record UpdateSceneTrackerRequest(
    string Time,
    string Location,
    string Weather,
    string[] CharactersPresent,
    JsonElement? AdditionalProperties = null);

public record UpdateMainCharacterTrackerRequest(
    JsonElement Tracker,
    string Description);

public record UpdateCharacterStateRequest(JsonElement Tracker);
