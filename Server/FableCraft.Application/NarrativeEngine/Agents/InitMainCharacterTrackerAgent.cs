using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class InitMainCharacterTrackerAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.InitMainCharacterTrackerAgent;

    public async Task Invoke(GenerationContext context,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.WorldSettings(context.PromptPath)}

                             {PromptSections.CurrentSceneTracker(context)}

                             {PromptSections.MainCharacter(context)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);

        var instruction = await dbContext.Adventures
            .Select(x => new { x.Id, x.FirstSceneGuidance })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
        var requestPrompt = $"""
                             {PromptSections.SceneContent(context.NewScene?.Scene)}

                             {PromptSections.InitialInstruction(instruction.FirstSceneGuidance)}

                             It's the first scene of the adventure. Initialize the tracker based on the scene content and characters description.
                             """;

        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = CreateOutputParser();
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        var kernelWithKg = kernel.Build();

        var tracker = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(InitMainCharacterTrackerAgent),
            kernelWithKg,
            cancellationToken);
        context.NewTracker!.MainCharacter = tracker;
    }

    private static Func<string, MainCharacterState>
        CreateOutputParser()
    {
        return response =>
        {
            var tracker = ResponseParser.ExtractJson<MainCharacterTracker>(response, "main_character_tracker");

            return new MainCharacterState
            {
                MainCharacter = tracker,
                MainCharacterDescription = null!
            };
        };
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        var options = PromptSections.GetJsonOptions();
        var structure = context.TrackerStructure;
        var trackerPrompt = GetSystemPrompt(structure);

        var storyBible = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "StoryBible.md"));
        var progressionSystem = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "ProgressionSystem.md"));

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.MainCharacterTrackerStructure, JsonSerializer.Serialize(trackerPrompt, options)),
            (PlaceholderNames.MainCharacterTrackerOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)),
            ("{{world_setting}}",
             File.Exists(Path.Combine(context.PromptPath, "WorldSettings.md")) ? File.ReadAllText(Path.Combine(context.PromptPath, "WorldSettings.md")) : string.Empty),
            ("{{character_definition}}", context.MainCharacter.Description),
            ("{{progression_system}}", progressionSystem),
            ("{{story_bible}}", storyBible)!);
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure) => TrackerExtensions.ConvertToOutputJson(structure.MainCharacter);

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure) => TrackerExtensions.ConvertToSystemJson(structure.MainCharacter);
}