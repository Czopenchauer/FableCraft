using System.Text;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Performs lightweight inference about a significant character's current state.
/// This is NOT full simulation - it answers: "Given who this person is and what's happened,
/// where are they now and what state are they in?"
/// </summary>
internal sealed class OffscreenInferenceAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.OffscreenInferenceAgent;

    public async Task<OffscreenInferenceOutput> Invoke(
        GenerationContext context,
        OffscreenInferenceInput input,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await GetPromptAsync(context);
        systemPrompt = PopulatePlaceholders(systemPrompt, input);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        // No plugins needed for inference - it's lightweight reasoning based on provided context
        Kernel kernel = kernelBuilder.Create().Build();

        var outputParser = ResponseParser.CreateJsonParser<OffscreenInferenceOutput>(
            "offscreen_inference",
            ignoreNull: true);

        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(OffscreenInferenceAgent),
            kernel,
            cancellationToken);
    }

    private string PopulatePlaceholders(string prompt, OffscreenInferenceInput input)
    {
        var jsonOptions = PromptSections.GetJsonOptions(ignoreNull: true);

        prompt = prompt.Replace(PlaceholderNames.CharacterName, input.Character.Name);

        prompt = prompt.Replace("{{core_profile}}", input.Character.CharacterState.ToJsonString(jsonOptions));

        var routine = input.Character.CharacterState.Routine;
        prompt = prompt.Replace("{{routine}}", routine?.ToJsonString(jsonOptions) ?? "No routine defined.");

        var goals = input.Character.CharacterState.Goals;
        prompt = prompt.Replace("{{active_projects}}", goals?.ToJsonString(jsonOptions) ?? "No active projects.");

        prompt = prompt.Replace("{{last_state}}", input.Character.CharacterTracker?.ToJsonString(jsonOptions) ?? "{}");

        prompt = prompt.Replace("{{time_elapsed}}", input.TimeElapsed);

        prompt = prompt.Replace("{{current_datetime}}", input.CurrentDateTime);

        prompt = prompt.Replace("{{world_events}}", FormatWorldEvents(input.WorldEvents));

        prompt = prompt.Replace("{{events_log}}", FormatEventsLog(input.EventsLog));

        return prompt;
    }

    private static string FormatWorldEvents(object? worldEvents)
    {
        if (worldEvents == null)
        {
            return "No significant world events during this period.";
        }

        return worldEvents.ToJsonString();
    }

    private static string FormatEventsLog(List<CharacterEventDto> events)
    {
        if (events.Count == 0)
        {
            return "No events recorded affecting this character.";
        }

        var sb = new StringBuilder();
        foreach (var evt in events.OrderBy(e => e.Time))
        {
            sb.AppendLine($"**{evt.Time}** (from {evt.SourceCharacter}):");
            sb.AppendLine($"- Event: {evt.Event}");
            sb.AppendLine($"- Their read: {evt.SourceRead}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
