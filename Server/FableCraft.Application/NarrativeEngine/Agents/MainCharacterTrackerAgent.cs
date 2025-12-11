using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class MainCharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory)
{
    public async Task<(CharacterTracker, string)> Invoke(
        GenerationContext context,
        Tracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.LlmPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure, x.AdventureStartTime })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var builder = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithStoryTracker(storyTrackerResult, true)
            .WithMainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription ?? context.MainCharacter.Description);

        if (isFirstScene)
        {
            builder
                .WithSceneContent(context.NewScene?.Scene)
                .WithUserMessage("It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.");
        }
        else
        {
            builder
                .WithMainCharacterTracker(context.SceneContext!, 1, true)
                .WithSceneContent(context.NewScene?.Scene)
                .WithLastScenes(context.SceneContext!, 5);
        }

        var chatHistory = builder.Build();

        var outputParser = ResponseParser.CreateJsonTextParser<CharacterTracker>("tracker", "character_description", true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        Kernel kernel = kernelBuilder.Create().Build();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(MainCharacterTrackerAgent),
            kernel,
            cancellationToken);
    }

    private async static Task<string> BuildInstruction(TrackerStructure trackerStructure)
    {
        var options = ChatHistoryBuilder.GetJsonOptions();
        var trackerPrompt = GetSystemPrompt(trackerStructure);

        return await PromptBuilder.BuildPromptAsync("MainCharacterTrackerPrompt.md",
            ("{{main_character_prompt}}", JsonSerializer.Serialize(trackerPrompt, options)),
            ("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(trackerStructure), options)));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
        => TrackerExtensions.ConvertToOutputJson(structure.MainCharacter);

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
        => TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
}
