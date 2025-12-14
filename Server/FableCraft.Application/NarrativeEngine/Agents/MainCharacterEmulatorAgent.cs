using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

public sealed class MainCharacterEmulationRequest
{
    public required Guid AdventureId { get; init; }

    public required string Instruction { get; init; }
}

public sealed class MainCharacterEmulationResponse
{
    public required string Text { get; init; }
}

public sealed class MainCharacterEmulatorAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory)
{
    private const int NumberOfScenesToInclude = 5;

    public async Task<MainCharacterEmulationResponse> InvokeAsync(
        MainCharacterEmulationRequest request,
        CancellationToken cancellationToken)
    {
        ProcessExecutionContext.AdventureId.Value = request.AdventureId;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var adventure = await dbContext.Adventures
            .Where(x => x.Id == request.AdventureId)
            .Include(x => x.MainCharacter)
            .Include(x => x.AgentLlmPresets)
            .ThenInclude(x => x.LlmPreset)
            .Select(x => new
            {
                x.MainCharacter,
                x.AgentLlmPresets,
                x.PromptPath
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (adventure == null)
        {
            throw new InvalidOperationException($"Adventure {request.AdventureId} not found");
        }

        var latestScene = await dbContext.Scenes
            .Where(x => x.AdventureId == request.AdventureId)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var recentScenes = await dbContext.Scenes
            .Where(x => x.AdventureId == request.AdventureId)
            .Include(x => x.CharacterActions)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(NumberOfScenesToInclude)
            .ToListAsync(cancellationToken);

        var agentPreset = adventure.AgentLlmPresets
            .SingleOrDefault(x => x.AgentName == AgentName.MainCharacterEmulatorAgent);

        if (agentPreset == null)
        {
            throw new InvalidOperationException(
                $"No LLM preset configured for {AgentName.MainCharacterEmulatorAgent}");
        }

        var kernelBuilder = kernelBuilderFactory.Create(agentPreset.LlmPreset);
        var kernel = kernelBuilder.Create().Build();

        var systemPrompt = await BuildSystemPrompt(adventure.PromptPath);

        var mainCharacterDescription = latestScene?.Metadata.Tracker?.MainCharacter?.MainCharacterDescription
                                       ?? adventure.MainCharacter.Description;

        var mainCharacterTracker = latestScene?.Metadata.Tracker?.MainCharacter;

        var contextPrompt = BuildContextPrompt(
            adventure.MainCharacter.Name,
            mainCharacterDescription,
            mainCharacterTracker,
            recentScenes);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(contextPrompt);
        chatHistory.AddUserMessage($"""
                                    <instruction>
                                    {request.Instruction}
                                    </instruction>

                                    Write the response from the perspective of {adventure.MainCharacter.Name}, staying true to their character, current emotional state, and the context of recent events.
                                    """);

        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();

        var result = await agentKernel.SendRequestAsync(
            chatHistory,
            text => text,
            promptExecutionSettings,
            nameof(MainCharacterEmulatorAgent),
            kernel,
            cancellationToken);

        return new MainCharacterEmulationResponse
        {
            Text = result
        };
    }

    private async static Task<string> BuildSystemPrompt(string promptPath)
    {
        var agentPromptPath = Path.Combine(promptPath, $"{AgentName.MainCharacterEmulatorAgent}.md");

        if (File.Exists(agentPromptPath))
        {
            return await File.ReadAllTextAsync(agentPromptPath);
        }

        return """
               You are a character emulator. Your task is to write text from the perspective of the main character.
               You will receive information about the character, their current state, and recent events.
               Based on the instruction provided, write a response that authentically represents how this character would express themselves.

               Guidelines:
               - Stay true to the character's personality, voice, and mannerisms
               - Consider their current emotional and mental state
               - Reference recent events when relevant
               - Write in first person from the character's perspective
               - Match the tone and style appropriate to the instruction (dialogue, inner thoughts, letter, etc.)
               """;
    }

    private static string BuildContextPrompt(
        string characterName,
        string characterDescription,
        MainCharacterTracker? tracker,
        List<Scene> recentScenes)
    {
        var scenesSection = string.Empty;
        if (recentScenes.Count > 0)
        {
            var formattedScenes = string.Join("\n\n---\n\n",
                recentScenes
                    .OrderBy(x => x.SequenceNumber)
                    .Select(s =>
                    {
                        var selectedAction = s.CharacterActions.FirstOrDefault(a => a.Selected)?.ActionDescription;
                        return $"""
                                SCENE {s.SequenceNumber}:
                                {s.NarrativeText}
                                {(selectedAction != null ? $"\nPlayer chose: {selectedAction}" : "")}
                                """;
                    }));

            scenesSection = $"""
                             <recent_scenes>
                             {formattedScenes}
                             </recent_scenes>
                             """;
        }

        var trackerSection = string.Empty;
        if (tracker != null)
        {
            return $"""
                              <character_state>
                              Current tracker information reflecting the character's state:
                              
                              {tracker.ToJsonString()}
                              </character_state>
                              
                              {scenesSection}
                              """;
        }

        return $"""
                <main_character>
                Name: {characterName}

                Description:
                {characterDescription}
                </main_character>

                {trackerSection}

                {scenesSection}
                """;
    }
}
