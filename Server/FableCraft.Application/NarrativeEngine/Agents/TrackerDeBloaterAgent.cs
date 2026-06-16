using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class TrackerDeBloaterAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.TrackerDeBloaterAgent;

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        var mainCharTracker = context.NewTracker?.MainCharacter?.MainCharacter;
        if (mainCharTracker is null)
        {
            logger.Warning("TrackerDeBloaterAgent: main character tracker is null, skipping de-bloat");
            return;
        }

        var agentName = GetAgentName();
        var preset = context.AgentLlmPreset.FirstOrDefault(x => x.AgentName == agentName);
        if (preset is null)
        {
            logger.Information("TrackerDeBloaterAgent: no LLM preset configured for this adventure, skipping de-bloat");
            return;
        }

        var kernelBuilder = await GetKernelBuilder(context);

        var systemPrompt = await BuildInstruction(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var trackerStateJson = mainCharTracker.ToJsonString();

        var requestPrompt = $"""
                             Previous tracker state to de-bloat:
                             {trackerStateJson}
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<TrackerDeBloaterDeltaOutput>("tracker");
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        var kernel = kernelBuilder.Create();
        var kernelWithKg = kernel.Build();

        var deltaOutput = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(TrackerDeBloaterAgent),
            kernelWithKg,
            cancellationToken);

        var mergedTracker = TrackerMerger.Merge(mainCharTracker, deltaOutput.Updates);

        context.NewTracker!.MainCharacter!.MainCharacter = mergedTracker;

        logger.Information("TrackerDeBloaterAgent delta merge completed. De-bloated for {Character}, updates applied: {UpdateCount} fields",
            context.MainCharacter.Name,
            CountUpdates(deltaOutput.Updates));
    }

    private static int CountUpdates(JsonElement updates)
    {
        if (updates.ValueKind != JsonValueKind.Object)
            return 0;

        return updates.EnumerateObject().Count();
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        var options = PromptSections.GetJsonOptions();
        var trackerDefinition = TrackerExtensions.ConvertToSystemJson(context.TrackerStructure.MainCharacter);

        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.TrackerDefinition, JsonSerializer.Serialize(trackerDefinition, options)),
            (PlaceholderNames.TrackerState, "Provided in the user message"));
    }
}