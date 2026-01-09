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

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Agent for simulating a character during cohort simulation.
/// Responds to Moderator queries (intention, response, reflection).
/// Maintains accumulated ChatHistory across multiple queries.
/// </summary>
internal sealed class CharacterSimulationAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    private const int MaxSceneHistoryCount = 20;

    protected override AgentName GetAgentName() => AgentName.CharacterSimulation;

    /// <summary>
    /// Response from a character query.
    /// </summary>
    internal sealed class CharacterQueryResponse
    {
        /// <summary>
        /// The character's prose response to the query.
        /// </summary>
        public required string ProseResponse { get; init; }

        /// <summary>
        /// The submitted reflection (if character called submit_reflection).
        /// Only populated during reflection queries.
        /// </summary>
        public StandaloneSimulationOutput? SubmittedReflection { get; init; }
    }

    public async Task<CharacterQueryResponse> InvokeQuery(
        GenerationContext context,
        CharacterContext character,
        CharacterQueryType queryType,
        string stimulus,
        string query,
        ChatHistory chatHistory,
        CohortSimulationInput cohortInput,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        if (chatHistory.Count == 0)
        {
            var systemPrompt = await GetPromptAsync(context);
            systemPrompt = await PopulateCharacterPlaceholders(systemPrompt, character, cohortInput, context);
            chatHistory.AddSystemMessage(systemPrompt);
        }

        var userMessage = BuildUserMessage(queryType, stimulus, query);
        chatHistory.AddUserMessage(userMessage);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);

        var toolsPlugin = await pluginFactory.CreateCharacterPluginAsync<CharacterSimulationToolsPlugin>(
            context,
            callerContext,
            character.CharacterId);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(toolsPlugin));

        Kernel builtKernel = kernel.Build();
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var response = await agentKernel.SendRequestAsync(
            chatHistory,
            raw => raw,
            promptExecutionSettings,
            $"{nameof(CharacterSimulationAgent)}_{character.Name}",
            builtKernel,
            cancellationToken);

        chatHistory.AddAssistantMessage(response);

        logger.Information(
            "Character {CharacterName} responded to {QueryType} query. Reflection submitted: {ReflectionSubmitted}",
            character.Name,
            queryType,
            toolsPlugin.SubmittedReflection != null);

        return new CharacterQueryResponse
        {
            ProseResponse = response,
            SubmittedReflection = toolsPlugin.SubmittedReflection
        };
    }

    private async Task<string> PopulateCharacterPlaceholders(
        string prompt,
        CharacterContext character,
        CohortSimulationInput cohortInput,
        GenerationContext context)
    {
        var jsonOptions = PromptSections.GetJsonOptions(ignoreNull: true);

        // CHARACTER_NAME
        prompt = prompt.Replace(PlaceholderNames.CharacterName, character.Name);

        // core_profile - the character's stable identity
        prompt = prompt.Replace("{{core_profile}}", character.CharacterState.ToJsonString(jsonOptions));

        // current_state - emotional landscape (extract from CharacterState if available)
        var currentState = ExtractCurrentState(character);
        prompt = prompt.Replace("{{current_state}}", currentState);

        // character_tracker - physical state
        prompt = prompt.Replace("{{character_tracker}}", character.CharacterTracker?.ToJsonString(jsonOptions) ?? "No physical state tracked.");

        // relationships
        prompt = prompt.Replace("{{relationships}}", FormatRelationships(character));

        prompt = prompt.Replace("{{recent_memories}}", BuildSceneHistoryContent(character));

        prompt = prompt.Replace("{{time_period}}", cohortInput.SimulationPeriod.ToJsonString(jsonOptions));

        prompt = prompt.Replace("{{world_events}}", cohortInput.WorldEvents?.ToJsonString(jsonOptions) ?? "No significant world events.");

        prompt = prompt.Replace("{{significant_characters}}", FormatSignificantCharacters(cohortInput.SignificantCharacters));

        // Replace injectable references (like dot_notation_reference, salience_scale, etc.)
        prompt = await ReplaceInjectableReference(prompt, "{{dot_notation_reference}}", "DotNotation.md", context.PromptPath);
        prompt = await ReplaceInjectableReference(prompt, "{{salience_scale}}", "Salience.md", context.PromptPath);
        prompt = await ReplaceInjectableReference(prompt, "{{physical_state_reference}}", "PhysicalStateReference.md", context.PromptPath);
        prompt = await ReplaceInjectableReference(prompt, "{{knowledge_boundaries}}", "KnowledgeBoundaries.md", context.PromptPath);

        return prompt;
    }

    private static string ExtractCurrentState(CharacterContext character)
    {
        // Try to extract psychology section from CharacterState extension data (new schema)
        if (character.CharacterState.ExtensionData?.TryGetValue("psychology", out var psychology) == true)
        {
            return psychology.ToJsonString();
        }

        return "Current psychological state not explicitly tracked.";
    }

    private static string BuildUserMessage(CharacterQueryType queryType, string stimulus, string query)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(stimulus))
        {
            sb.AppendLine($"**Stimulus:** {stimulus}");
            sb.AppendLine();
        }

        sb.AppendLine($"**Query ({queryType}):** {query}");

        return sb.ToString();
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
            if (scene.SceneTracker != null)
            {
                sb.AppendLine($"Time: {scene.SceneTracker.Time}");
                sb.AppendLine($"Location: {scene.SceneTracker.Location}");
            }

            sb.AppendLine();
            sb.AppendLine(scene.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatSignificantCharacters(string[]? names)
    {
        if (names == null || names.Length == 0)
        {
            return "No significant characters available for interaction.";
        }

        return string.Join(", ", names);
    }

    private static async Task<string> ReplaceInjectableReference(string prompt, string placeholder, string fileName, string promptPath)
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
}