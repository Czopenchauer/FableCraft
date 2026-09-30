using System.Text;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Application.NarrativeEngine.Workflow;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Post-scene lore minter. Consumes the finished scene and mints new world canon
///     (locations, NPCs, factions, ecology, customs, events, rumors) into the lorebook.
///     Runs after the Writer, alongside the WorldInfoExtractor, in the enrichment phase.
/// </summary>
internal sealed class LoreAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory), IProcessor
{
    private const int MaxLoreItems = 3;

    protected override AgentName GetAgentName() => AgentName.LoreAgent;

    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.NewLore.Count > 0)
        {
            logger.Information("Lore already created, skipping ({Count})", context.NewLore.Count);
            return;
        }

        if (string.IsNullOrWhiteSpace(context.NewScene?.Scene))
        {
            logger.Information("LoreAgent: no scene content available, skipping");
            return;
        }

        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);
        systemPrompt = systemPrompt.Replace(PlaceholderNames.CharacterName, context.MainCharacter.Name);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(BuildContextPrompt(context));
        chatHistory.AddUserMessage(BuildRequestPrompt(context));

        if (!string.IsNullOrWhiteSpace(context.LoreInstruction))
        {
            chatHistory.AddUserMessage($"""
                                        <player_instruction>
                                        The player provided the following additional instruction for this generation. Treat it as a high-priority direction when mining new lore from the scene:
                                        {context.LoreInstruction}
                                        </player_instruction>
                                        """);
        }

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType().Name, context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        var kernelWithPlugins = kernel.Build();

        var lore = await agentKernel.SendRequestAsync(
            chatHistory,
            ParseNewLore,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(LoreAgent),
            kernelWithPlugins,
            cancellationToken);

        if (lore.Length == 0)
        {
            logger.Information("LoreAgent: no new lore this cycle");
            return;
        }

        if (lore.Length > MaxLoreItems)
        {
            logger.Warning("LoreAgent produced {Count} items, trimming to {Max}", lore.Length, MaxLoreItems);
            lore = lore.Take(MaxLoreItems).ToArray();
        }

        lock (context)
        {
            context.NewLore.AddRange(lore);
        }

        logger.Information("Created {Count} new lore", lore.Length);
    }

    private static string BuildContextPrompt(GenerationContext context)
    {
        var sceneTrackerSection = context.NewTracker?.Scene != null
            ? PromptSections.SceneTracker(context, context.NewTracker.Scene)
            : string.Empty;

        return $"""
                {sceneTrackerSection}

                {PromptSections.Context(context)}

                {PromptSections.NarrativeCatalystGuidance(context)}
                """;
    }

    private static string BuildRequestPrompt(GenerationContext context)
    {
        return $"""
                {PromptSections.CurrentScene(context)}

                Mine this finished scene for new world lore. Work through all seven reasoning steps in <think>, then emit your output in <new_lore> exactly as specified.
                """;
    }

    private static GeneratedLore[] ParseNewLore(string response)
    {
        var content = ResponseParser.ExtractText(response, "new_lore");

        if (content.Contains("No new lore this cycle", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var items = new List<GeneratedLore>();
        string? currentTitle = null;
        var currentBody = new StringBuilder();

        void Flush()
        {
            var body = currentBody.ToString().Trim();
            if (!string.IsNullOrEmpty(currentTitle) && !string.IsNullOrEmpty(body))
            {
                items.Add(new GeneratedLore { Title = currentTitle, Description = body });
            }

            currentBody.Clear();
        }

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                Flush();
                currentTitle = line[4..].Trim();
            }
            else
            {
                currentBody.AppendLine(line);
            }
        }

        Flush();

        return items.ToArray();
    }
}
