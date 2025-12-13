using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class StoryTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory)
{
    public async Task<Tracker> Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.LlmPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure, x.AdventureStartTime })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);
        var isFirstScene = (context.SceneContext?.Length ?? 0) == 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.MainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription)}

                             {PromptSections.ExistingCharacters(context.Characters)}

                             {(!isFirstScene ? PromptSections.LastScenes(context.SceneContext!, 5) : "")}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        string requestPrompt;
        if (isFirstScene)
        {
            requestPrompt = $"""
                             {PromptSections.SceneContent(context.NewScene!.Scene)}

                             It's the first scene of the adventure. Initialize the tracker based on the scene content.
                             """;
        }
        else
        {
            requestPrompt = $"""
                             {(context.NewCharacters?.Length > 0 ? PromptSections.NewCharacters(context.NewCharacters) : "")}

                             {(context.SceneContext?.Length == 1 ? PromptSections.AdventureStartTime(trackerStructure.AdventureStartTime) : "")}

                             {(context.NewLocations?.Length > 0 ? PromptSections.NewLocations(context.NewLocations) : "")}

                             {(!isFirstScene ? PromptSections.PreviousStoryTrackers(context.SceneContext!) : "")}

                             THIS IS CURRENT SCENE! Update the story tracker based on the scene content and previous trackers.
                             {PromptSections.CurrentScene(context.NewScene!.Scene)}
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<Tracker>("tracker", true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        Kernel kernel = kernelBuilder.Create().Build();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(StoryTrackerAgent),
            kernel,
            cancellationToken);
    }

    private async static Task<string> BuildInstruction(TrackerStructure trackerStructure)
    {
        JsonSerializerOptions options = PromptSections.GetJsonOptions();
        var trackerPrompt = GetSystemPrompt(trackerStructure);

        return await PromptBuilder.BuildPromptAsync("StoryTrackerPrompt.md",
            ("{{field_update_logic}}", JsonSerializer.Serialize(trackerPrompt[nameof(Tracker.Story)], options)),
            ("{{json_output_format}}", JsonSerializer.Serialize(GetOutputJson(trackerStructure), options)));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        var dictionary = new Dictionary<string, object>();
        var story = TrackerExtensions.ConvertToOutputJson(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);
        dictionary.Add(nameof(Tracker.CharactersPresent), structure.CharactersPresent.DefaultValue ?? Array.Empty<string>());
        return dictionary;
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        var dictionary = new Dictionary<string, object>();
        var story = TrackerExtensions.ConvertToSystemJson(structure.Story);
        dictionary.Add(nameof(Tracker.Story), story);
        dictionary.Add(nameof(Tracker.CharactersPresent),
            new
            {
                structure.CharactersPresent.Prompt,
                structure.CharactersPresent.DefaultValue,
                structure.CharactersPresent.ExampleValues
            });
        return dictionary;
    }
}