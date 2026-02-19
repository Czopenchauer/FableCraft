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

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class WriterAgent : BaseAgent, IProcessor
{
    private const int SceneContextCount = 30;
    private readonly IAgentKernel _agentKernel;
    private readonly IPluginFactory _pluginFactory;

    public WriterAgent(IAgentKernel agentKernel,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IPluginFactory pluginFactory) : base(dbContextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _pluginFactory = pluginFactory;
    }

    public async Task Invoke(
        GenerationContext context,
        CancellationToken cancellationToken)
    {
        if (context.NewScene is not null)
        {
            return;
        }

        context.CharacterEmulationOutputs.Clear();

        var kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);
        systemPrompt = systemPrompt.Replace(PlaceholderNames.CharacterName, context.MainCharacter.Name);
        var hasSceneContext = context.SceneContext.Length > 0;

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.MainCharacter(context)}

                             {PromptSections.MainCharacterTracker(context.SceneContext)}

                             {PromptSections.Context(context)}

                             {PromptSections.CurrentSceneTracker(context)}

                             {PromptSections.PreviousCharacterObservations(context.SceneContext)}
                             """;
        chatHistory.AddUserMessage($"""
                                    {PromptSections.CharacterForEmulation(context.Characters, context)}

                                    {PromptSections.BackgroundCharacterProfiles(context.BackgroundCharacters)}
                                    """);
        chatHistory.AddUserMessage(contextPrompt);
        foreach (string se in context.SceneContext
                     .OrderByDescending(x => x.SequenceNumber)
                     .Take(SceneContextCount)
                     .OrderBy(x => x.SequenceNumber)
                     .Select(x => $"""
                                   Time: {x.Metadata.Tracker!.Scene!.Time}
                                   Location: {x.Metadata.Tracker.Scene.Location}
                                   Weather: {x.Metadata.Tracker.Scene.Weather}
                                   Characters: {string.Join(", ", x.Metadata.Tracker.Scene.CharactersPresent)}
                                   {x.SceneContent}
                                   """))
        {
            chatHistory.AddUserMessage(se);
        }

        string requestPrompt;
        if (!hasSceneContext)
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);

            var instruction = await dbContext.Adventures
                .Select(x => new
                {
                    x.Id,
                    x.FirstSceneGuidance
                })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
            requestPrompt = $"""
                             {PromptSections.ResolutionOutput(context.NewResolution)}

                             {PromptSections.ExtraLoreEntries(context.ExtraLoreEntries)}

                             {PromptSections.InitialInstruction(instruction.FirstSceneGuidance)}

                             Generate a detailed scene based on the above resolution and context.
                             """;
        }
        else
        {
            requestPrompt = $"""
                             {PromptSections.ChroniclerGuidance(context.SceneContext)}

                             {GetPendingMcInteractions(context)}

                             {PromptSections.PlayerAction(context.PlayerAction)}

                             ---
                             Ensure the output is wrapped in correct XML tags. Remember about the <scene> tag!
                             ## Quick Reference
                             
                             **Emulation:**
                             - Call for EVERY full-profile character, EVERY beat
                             - Multiple calls per scene is normal and expected
                             - Sanitize situations: no self-reference, no assessments, no "helping/threatening/intense"—pure observable actions
                             - Speech is verbatim. Actions rendered through MC's perception.
                             - If emulation contradicts your plan, emulation wins.
                             
                             **MC Agency:**
                             - MC does ONLY what player input specified—nothing more
                             - No invented dialogue, decisions, or "helpful" additional actions
                             - Wishful thinking ("I convince," "knowing this will earn trust") = inner monologue, not world effect
                             - No mechanism = MC acts, world doesn't bend
                             
                             **Never invent. Always ask.**
                             Generate a detailed scene based on the above resolution and context.
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId, context.NewSceneId);
        await _pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await _pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        await _pluginFactory.AddPluginAsync<CharacterEmulationPlugin>(kernel, context, callerContext);
        var kernelWithKg = kernel.Build();

        var outputParser = CreateOutputParser();

        var newScene = await _agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(WriterAgent),
            kernelWithKg,
            cancellationToken,
            new AgentKernelOptions { MaxParsingRetries = 1 });

        context.NewScene = newScene;
    }

    protected override AgentName GetAgentName() => AgentName.WriterAgent;

    private static Func<string, GeneratedScene> CreateOutputParser()
    {
        return response =>
        {
            var scene = ResponseParser.ExtractText(response, "scene");
            var choicesText = ResponseParser.ExtractText(response, "choices");
            var choices = choicesText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            CreationRequests? creationRequests = null;
            if (response.Contains("creation_requests"))
            {
                creationRequests = ResponseParser.TryExtractJson<CreationRequests?>(response, "creation_requests", ignoreNull: true);
            }

            ImportanceFlags? importanceFlags = null;
            if (response.Contains("importance_flags"))
            {
                importanceFlags = ResponseParser.TryExtractJson<ImportanceFlags?>(response, "importance_flags", ignoreNull: true);
            }

            return new GeneratedScene
            {
                Scene = scene,
                Choices = choices,
                CreationRequests = creationRequests,
                ImportanceFlags = importanceFlags
            };
        };
    }

    /// <summary>
    ///     Builds a prompt section for pending MC interactions from characters who want to seek the MC.
    ///     Characters with high/immediate urgency should be woven into the upcoming scene.
    /// </summary>
    private static string GetPendingMcInteractions(GenerationContext context)
    {
        var pendingInteractions = context.Characters
            .Where(c => c.SimulationMetadata?.PendingMcInteraction?.ExtensionData != null)
            .Select(c => new
            {
                Character = c.Name,
                Data = c.SimulationMetadata!.PendingMcInteraction!.ExtensionData!
            })
            .ToList();

        if (pendingInteractions.Count == 0)
        {
            return string.Empty;
        }

        var formatted = string.Join("\n\n",
            pendingInteractions.Select(p => $"""
                                             **{p.Character}**)
                                             {p.Data}
                                             """.Trim()));

        return $"""
                <pending_mc_interactions>
                The following characters have decided to seek out the MC. Consider weaving them into the scene based on urgency:
                {formatted}
                </pending_mc_interactions>
                """;
    }
}