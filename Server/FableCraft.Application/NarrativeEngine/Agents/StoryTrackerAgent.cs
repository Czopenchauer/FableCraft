using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

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

        var builder = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithMainCharacter(context.MainCharacter, context.LatestSceneContext?.Metadata.MainCharacterDescription)
            .WithExistingCharacters(context.Characters);

        if (context.NewCharacters?.Length > 0)
        {
            builder.WithTaggedContent("new_characters",
                $"<character>\n{string.Join("\n\n", context.NewCharacters.Select(c => $"{c.Name}\n{c.Description}"))}\n</character>");
        }

        if (context.SceneContext?.Length == 1)
        {
            builder.WithTaggedContent("adventure_start_time", trackerStructure.AdventureStartTime);
        }

        if (context.NewLocations?.Length > 0)
        {
            builder.WithNewLocations(context.NewLocations);
        }

        if (isFirstScene)
        {
            builder
                .WithSceneContent(context.NewScene!.Scene)
                .WithUserMessage("It's the first scene of the adventure. Initialize the tracker based on the scene content.");
        }
        else
        {
            var options = ChatHistoryBuilder.GetJsonOptions(true);
            var previousStoryTrackers = string.Join("\n\n", (context.SceneContext ?? [])
                .OrderByDescending(x => x.SequenceNumber)
                .Where(x => x.Metadata.Tracker != null)
                .Take(1)
                .Select(s => JsonSerializer.Serialize(s.Metadata.Tracker?.Story, options)));

            builder
                .WithTaggedContent("previous_trackers", previousStoryTrackers)
                .WithLastScenes(context.SceneContext!, 5)
                .WithUserMessage("THIS IS CURRENT SCENE! Update the story tracker based on the scene content and previous trackers.")
                .WithCurrentScene(context.NewScene!.Scene);
        }

        var chatHistory = builder.Build();

        var outputParser = ResponseParser.CreateJsonParser<Tracker>("tracker", true);
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
        dictionary.Add(nameof(Tracker.CharactersPresent), new
        {
            structure.CharactersPresent.Prompt,
            structure.CharactersPresent.DefaultValue,
            structure.CharactersPresent.ExampleValues
        });
        return dictionary;
    }
}
