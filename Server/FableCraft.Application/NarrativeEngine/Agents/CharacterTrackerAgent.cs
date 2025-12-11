using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class CharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<(CharacterTracker, string)> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        Tracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(generationContext.LlmPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == generationContext.AdventureId, cancellationToken);

        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure, context.Name);

        var chatHistory = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithStoryTracker(storyTrackerResult, true)
            .WithCharacterStateContext(context, true)
            .WithRecentScenesForCharacter(
                generationContext.SceneContext ?? [],
                generationContext.MainCharacter.Name,
                context.Name,
                3)
            .WithCurrentScene(generationContext.NewScene?.Scene)
            .Build();

        var outputParser = ResponseParser.CreateJsonTextParser<CharacterTracker>("character_tracker", "character_description", true);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(CharacterTrackerAgent),
            kernelWithKg,
            cancellationToken);
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure, string characterName)
    {
        var options = ChatHistoryBuilder.GetJsonOptions();

        var prompt = await PromptBuilder.BuildPromptAsync("CharacterTrackerAgentPrompt.md");
        return prompt
            .Replace("{{character_tracker_structure}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{character_tracker}}", JsonSerializer.Serialize(GetOutputJson(structure), options))
            .Replace("{CHARACTER_NAME}", characterName);
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
        => TrackerExtensions.ConvertToOutputJson(structure.Characters);

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
        => TrackerExtensions.ConvertToSystemJson(structure.Characters);
}
