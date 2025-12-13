using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class CharacterDevelopmentAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.CharacterDevelopmentAgent;

    public async Task<CharacterDevelopmentTracker?> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        StoryTracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(generationContext);

        if (generationContext.TrackerStructure.CharacterDevelopment == null || generationContext.TrackerStructure.CharacterDevelopment.Length == 0)
        {
            return null;
        }

        var systemPrompt = await BuildInstruction(generationContext, context.Name);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.StoryTracker(storyTrackerResult, true)}

                             {PromptSections.NewItems(generationContext.NewItems)}

                             {PromptSections.RecentScenesForCharacter(
                                 generationContext.SceneContext,
                                 generationContext.MainCharacter.Name,
                                 context.Name)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.CharacterStateContext(context)}

                             {PromptSections.CurrentScene(generationContext.NewScene?.Scene)}
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterDevelopmentTracker>("character_development", true);

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
            nameof(CharacterDevelopmentAgent),
            kernelWithKg,
            cancellationToken);
    }

    private async Task<string> BuildInstruction(GenerationContext context, string characterName)
    {
        JsonSerializerOptions options = PromptSections.GetJsonOptions();
        var structure = context.TrackerStructure;
        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.CharacterDevelopmentStructure, JsonSerializer.Serialize(GetSystemPrompt(structure), options)),
            (PlaceholderNames.CharacterDevelopmentOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)),
            (PlaceholderNames.CharacterName, characterName));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        if (structure.CharacterDevelopment == null || structure.CharacterDevelopment.Length == 0)
        {
            return new Dictionary<string, object>();
        }

        return TrackerExtensions.ConvertToOutputJson(structure.CharacterDevelopment);
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        if (structure.CharacterDevelopment == null || structure.CharacterDevelopment.Length == 0)
        {
            return new Dictionary<string, object>();
        }

        return TrackerExtensions.ConvertToSystemJson(structure.CharacterDevelopment);
    }
}