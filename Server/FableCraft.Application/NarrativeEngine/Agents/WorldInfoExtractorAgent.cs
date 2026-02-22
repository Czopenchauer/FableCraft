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

internal sealed class WorldInfoExtractorAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.WorldInfoExtractorAgent;

    public async Task<WorldInfoExtractionOutput> Invoke(
        GenerationContext context,
        string narrativeText,
        SceneTracker sceneTracker,
        AlreadyHandledContent alreadyHandledContent,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(BuildContextPrompt(context, sceneTracker, alreadyHandledContent));
        chatHistory.AddUserMessage(BuildRequestPrompt(narrativeText));

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType().Name, context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        var kernelWithPlugins = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<WorldInfoExtractionOutput>("world_info", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(WorldInfoExtractorAgent),
            kernelWithPlugins,
            cancellationToken);
    }

    private string BuildContextPrompt(
        GenerationContext context,
        SceneTracker sceneTracker,
        AlreadyHandledContent alreadyHandledContent)
    {
        var worldSettings = PromptSections.WorldSettings(context.PromptPath);

        return $"""
                <metadata>
                Location: {sceneTracker.Location}
                Time: {sceneTracker.Time}
                Characters Present: {string.Join(", ", sceneTracker.CharactersPresent ?? [])}
                </metadata>

                {worldSettings}

                {FormatAlreadyHandledContent(alreadyHandledContent)}
                """;
    }

    private static string BuildRequestPrompt(string narrativeText)
    {
        return $"""
                Extract the activity trail and world facts from the following narrative. Use exact timestamps and full location paths. Focus on observable actions and information exchanges.

                <narrative>
                {narrativeText}
                </narrative>
                """;
    }

    private static string FormatAlreadyHandledContent(AlreadyHandledContent content)
    {
        var sections = new List<string>();

        if (content.Characters is { Count: > 0 })
        {
            var characterNames = string.Join(", ", content.Characters.Select(c => c.AdditionalData.GetValueOrDefault("name")?.ToString() ?? "unknown"));
            sections.Add($"Characters being created: {characterNames}");
        }

        if (content.Locations is { Count: > 0 })
        {
            var locationNames = string.Join(", ", content.Locations.Select(l => l.AdditionalData.GetValueOrDefault("name")?.ToString() ?? "unknown"));
            sections.Add($"Locations being created: {locationNames}");
        }

        if (content.Items is { Count: > 0 })
        {
            var itemNames = string.Join(", ", content.Items.Select(i => i.AdditionalData.GetValueOrDefault("name")?.ToString() ?? "unknown"));
            sections.Add($"Items being created: {itemNames}");
        }

        if (content.Lore is { Count: > 0 })
        {
            var loreTopics = string.Join(", ", content.Lore.Select(l => l.AdditionalData.GetValueOrDefault("topic")?.ToString() ?? "unknown"));
            sections.Add($"Lore being created: {loreTopics}");
        }

        if (content.WorldEvents is { Count: > 0 })
        {
            var events = string.Join("\n", content.WorldEvents.Select(e => $"- [{e.When}] {e.Where}: {e.Event}"));
            sections.Add($"World events already tracked:\n{events}");
        }

        if (content.BackgroundCharacters is { Count: > 0 })
        {
            var bgNames = string.Join(", ", content.BackgroundCharacters.Select(b => b.Name));
            sections.Add($"Background characters being tracked: {bgNames}");
        }

        if (sections.Count == 0)
        {
            return """
                   <already_handled>
                   No content has been flagged as already handled.
                   </already_handled>
                   """;
        }

        return $"""
                <already_handled>
                The following content is already being committed to the knowledge graph. Do NOT re-extract:

                {string.Join("\n\n", sections)}
                </already_handled>
                """;
    }
}
