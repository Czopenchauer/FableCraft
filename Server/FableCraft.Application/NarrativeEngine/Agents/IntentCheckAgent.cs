using System.Text;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
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
/// Agent that determines a character's intentions for an upcoming period.
/// Used as a plugin by SimulationPlanner to understand what characters plan to do.
/// </summary>
internal sealed class IntentCheckAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.IntentCheckAgent;

    public async Task<IntentCheckOutput> Invoke(
        GenerationContext context,
        IntentCheckInput input,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);
        systemPrompt = PopulatePlaceholders(systemPrompt, input);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage($"""
                                    {BuildMemoriesSection(input.Character)}

                                    {PromptSections.RecentScenesForCharacter(input.Character, 20)}
                                    """
        );

        Kernel kernel = kernelBuilder.Create().Build();

        var outputParser = ResponseParser.CreateJsonParser<IntentCheckOutput>("intent", ignoreNull: true);
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(IntentCheckAgent),
            kernel,
            cancellationToken);
    }

    private string PopulatePlaceholders(string prompt, IntentCheckInput input)
    {
        var jsonOptions = PromptSections.GetJsonOptions(ignoreNull: true);
        var character = input.Character;

        prompt = prompt.Replace(PlaceholderNames.CharacterName, character.Name);
        prompt = prompt.Replace("{{core_profile}}", character.CharacterState.ToJsonString(jsonOptions));
        prompt = prompt.Replace("{{character_tracker}}", character.CharacterTracker?.ToJsonString(jsonOptions) ?? "{}");
        prompt = prompt.Replace("{{relationships}}", FormatRelationships(character));
        prompt = prompt.Replace("{{time_period}}", input.TimePeriod);
        prompt = prompt.Replace("{{arc_important_list}}", FormatArcImportantList(input.ArcImportantCharacters));
        prompt = prompt.Replace("{{world_events}}", input.WorldEvents ?? "No significant world events.");
        prompt = prompt.Replace("{{previous_intentions}}", input.PreviousIntentions ?? "No previous intentions recorded.");

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
            sb.AppendLine($"**{rel.TargetCharacterName}:**");
            sb.AppendLine(rel.Data.ToJsonString());
            var dynamicStr = rel.Dynamic?.ToString();
            if (!string.IsNullOrEmpty(dynamicStr))
            {
                sb.AppendLine($"Dynamic: {dynamicStr}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string BuildMemoriesSection(CharacterContext context)
    {
        if (context.CharacterMemories.Count == 0)
        {
            return string.Empty;
        }

        var memoriesText = string.Join("\n",
            context.CharacterMemories.Select(m => $"- [Time: {m.SceneTracker.Time}, Location: {m.SceneTracker.Location}] {m.MemoryContent} [{m.Data.ToJsonString()}]"));

        return $"""
                <character_memories>
                These are the {context.Name}'s memories from past scenes (ordered by recency):
                {memoriesText}
                </character_memories>
                """;
    }

    private static string FormatArcImportantList(string[] characters)
    {
        if (characters.Length == 0)
        {
            return "No arc-important characters available.";
        }

        return string.Join("\n", characters.Select(c => $"- {c}"));
    }
}