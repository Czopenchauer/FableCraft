using System.Text;

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

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Simulates a single arc_important character during off-screen time periods.
/// The character lives through the time period, pursuing goals, handling problems,
/// and potentially deciding to seek out the MC or other profiled characters.
/// </summary>
internal sealed class StandaloneSimulationAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    private const int MaxSceneHistoryCount = 20;

    protected override AgentName GetAgentName() => AgentName.StandaloneSimulationAgent;

    public async Task<StandaloneSimulationOutput> Invoke(
        GenerationContext context,
        StandaloneSimulationInput input,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);
        systemPrompt = PopulatePlaceholders(systemPrompt, input);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(BuildSceneHistoryPrompt(input.Character));

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);

        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);

        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(
            kernel,
            context,
            callerContext,
            input.Character.CharacterId);

        Kernel builtKernel = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<StandaloneSimulationOutput>(
            "solo_simulation",
            ignoreNull: true);

        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(StandaloneSimulationAgent),
            builtKernel,
            cancellationToken);
    }

    private string PopulatePlaceholders(string prompt, StandaloneSimulationInput input)
    {
        var jsonOptions = PromptSections.GetJsonOptions(ignoreNull: true);

        prompt = prompt.Replace(PlaceholderNames.CharacterName, input.Character.Name);

        prompt = prompt.Replace("{{core_profile}}", input.Character.CharacterState.ToJsonString(jsonOptions));
        prompt = prompt.Replace("{{character_tracker}}", input.Character.CharacterTracker?.ToJsonString(jsonOptions) ?? "{}");
        prompt = prompt.Replace("{{relationships}}", FormatRelationships(input.Character));
        prompt = prompt.Replace("{{time_period}}", input.TimePeriod);
        prompt = prompt.Replace("{{world_events}}", FormatWorldEvents(input.WorldEvents));

        return prompt;
    }

    private static string FormatRelationships(CharacterContext character)
    {
        if (character.Relationships.Count == 0)
        {
            return "No established relationships.";
        }

        var sb = new StringBuilder();
        foreach (var rel in character.Relationships)
        {
            sb.AppendLine($"### {rel.TargetCharacterName}");
            sb.AppendLine($"**Dynamic:** {rel.Dynamic}");
            if (rel.Data.Count > 0)
            {
                sb.AppendLine($"**Details:** {rel.Data.ToJsonString(PromptSections.GetJsonOptions(ignoreNull: true))}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatWorldEvents(object? worldEvents)
    {
        if (worldEvents == null)
        {
            return "No significant world momentum.";
        }

        return worldEvents.ToJsonString();
    }

    private static string FormatProfiledCharacters(string[]? names, string excludeName)
    {
        if (names == null || names.Length == 0)
        {
            return "None";
        }

        var filtered = names.Where(n => n != excludeName).ToArray();
        return filtered.Length > 0 ? string.Join(", ", filtered) : "None";
    }

    private static string BuildSceneHistoryPrompt(CharacterContext character)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Your Recent Scenes");
        sb.AppendLine();
        sb.AppendLine("These are scenes from your perspective (your memories of recent events). This simulation continues from where you left off.");
        sb.AppendLine();

        if (character.SceneRewrites.Count == 0)
        {
            sb.AppendLine("*No previous scenes recorded.*");
            return sb.ToString();
        }

        var recentScenes = character.SceneRewrites
            .OrderByDescending(s => s.SequenceNumber)
            .Take(MaxSceneHistoryCount)
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        foreach (var scene in recentScenes)
        {
            sb.AppendLine("---");
            sb.AppendLine($"**Scene {scene.SequenceNumber}**");
            if (scene.StoryTracker != null)
            {
                sb.AppendLine($"Time: {scene.StoryTracker.Time}");
                sb.AppendLine($"Location: {scene.StoryTracker.Location}");
            }

            sb.AppendLine();
            sb.AppendLine(scene.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}