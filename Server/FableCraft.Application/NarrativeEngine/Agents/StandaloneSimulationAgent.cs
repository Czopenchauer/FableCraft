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
        systemPrompt = await PopulateSystemPlaceholders(systemPrompt, input, context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(BuildContextPrompt(input, context));
        chatHistory.AddUserMessage(BuildRequestPrompt(input));

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

    private async Task<string> PopulateSystemPlaceholders(string prompt, StandaloneSimulationInput input, GenerationContext context)
    {
        var characterName = input.Character.Name;

        prompt = prompt.Replace(PlaceholderNames.CharacterName, characterName);

        prompt = await ReplaceInjectableReference(prompt, "{{dot_notation_reference}}", "DotNotation.md", context.PromptPath);
        prompt = await ReplaceInjectableReference(prompt, "{{salience_scale}}", "Salience.md", context.PromptPath);
        prompt = await ReplaceInjectableReference(prompt, "{{physical_state_reference}}", "PhysicalStateReference.md", context.PromptPath);
        prompt = await ReplaceInjectableReference(prompt, "{{knowledge_boundaries}}", "KnowledgeBoundaries.md", context.PromptPath);

        return prompt;
    }

    private async static Task<string> ReplaceInjectableReference(string prompt, string placeholder, string fileName, string promptPath)
    {
        if (!prompt.Contains(placeholder))
        {
            return prompt;
        }

        var filePath = Path.Combine(promptPath, fileName);
        if (File.Exists(filePath))
        {
            var fileContent = await File.ReadAllTextAsync(filePath);
            return prompt.Replace(placeholder, fileContent);
        }

        return prompt;
    }

    private string BuildContextPrompt(StandaloneSimulationInput input, GenerationContext context)
    {
        var jsonOptions = PromptSections.GetJsonOptions(ignoreNull: true);
        var characterName = input.Character.Name;

        var arcImportantNames = context.Characters
            .Where(c => c.Importance == CharacterImportance.ArcImportance && c.Name != characterName)
            .Select(c => c.Name)
            .ToArray();

        var significantNames = context.Characters
            .Where(c => c.Importance == CharacterImportance.Significant)
            .Select(c => c.Name)
            .ToArray();

        return $"""
            <identity>
            {input.Character.CharacterState.ToJsonString(jsonOptions)}
            </identity>

            <physical_state>
            {input.Character.CharacterTracker?.ToJsonString(jsonOptions) ?? "{}"}
            </physical_state>

            <relationships>
            {FormatRelationships(input.Character)}
            </relationships>

            <world_events>
            {FormatWorldEvents(input.WorldEvents)}
            </world_events>

            <available_npcs>
            **Arc-important characters** (cannot interact!):
            {FormatProfiledCharacters(arcImportantNames, characterName)}

            **Significant characters** (can interact with, log interactions to character_events):
            {FormatProfiledCharacters(significantNames, characterName)}
            </available_npcs>

            <last_scenes>
            {BuildSceneHistoryContent(input.Character)}
            </last_scenes>
            """;
    }

    private static string BuildRequestPrompt(StandaloneSimulationInput input)
    {
        return $"""
            Live through the period: {input.TimePeriod}

            What do you do? What happens? How are you affected?
            """;
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

    private static string BuildSceneHistoryContent(CharacterContext character)
    {
        if (character.SceneRewrites.Count == 0)
        {
            return "*No previous scenes recorded.*";
        }

        var sb = new StringBuilder();
        sb.AppendLine("These are scenes from your perspective (your memories of recent events). This simulation continues from where you left off.");
        sb.AppendLine();

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