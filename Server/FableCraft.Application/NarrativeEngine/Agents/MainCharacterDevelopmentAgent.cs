using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class MainCharacterDevelopmentAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory)
{
    public async Task<CharacterDevelopmentTracker?> Invoke(
        GenerationContext context,
        Tracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        if (trackerStructure.TrackerStructure.MainCharacterDevelopment == null ||
            trackerStructure.TrackerStructure.MainCharacterDevelopment.Length == 0)
        {
            return null;
        }

        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);
        var previousTracker = context.SceneContext
            .Where(x => x.Metadata.Tracker != null)
            .OrderByDescending(x => x.SequenceNumber)
            .FirstOrDefault()?.Metadata.Tracker;

        var chatHistory = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithStoryTracker(storyTrackerResult, true)
            .WithMainCharacterTracker(previousTracker?.MainCharacter, true)
            .WithPreviousDevelopment(previousTracker?.MainCharacterDevelopment, true)
            .WithRecentScenes(context.SceneContext ?? [], 3)
            .WithCurrentScene(context.NewScene?.Scene)
            .Build();

        var outputParser = ResponseParser.CreateJsonParser<CharacterDevelopmentTracker>("tracker", true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        Kernel kernel = kernelBuilder.Create().Build();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(MainCharacterDevelopmentAgent),
            kernel,
            cancellationToken);
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure)
    {
        var options = ChatHistoryBuilder.GetJsonOptions();

        return await PromptBuilder.BuildPromptAsync("MainCharacterDevelopmentAgentPrompt.md",
            ("{{main_character_prompt}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options)),
            ("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(structure), options)));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToOutputJson(structure.MainCharacterDevelopment!);
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToSystemJson(structure.MainCharacterDevelopment!);
    }
}
