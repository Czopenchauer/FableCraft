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
    public const int SceneContextCount = 30;
    private readonly IAgentKernel _agentKernel;
    private readonly IPluginFactory _pluginFactory;
    private readonly DispatchService _dispatchService;

    public WriterAgent(IAgentKernel agentKernel,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IPluginFactory pluginFactory,
        DispatchService dispatchService) : base(dbContextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _pluginFactory = pluginFactory;
        _dispatchService = dispatchService;
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
        foreach (GatheredCoLocatedCharacter gatheredContextCoLocatedCharacter in context.SceneContext
                     .OrderByDescending(x => x.SequenceNumber)
                     .FirstOrDefault()?.Metadata!.GatheredContext!.CoLocatedCharacters!)
        {
            context.LatestTracker()!.Scene!.CharactersPresent = context.LatestTracker()!.Scene!.CharactersPresent.Append(gatheredContextCoLocatedCharacter.Name).ToArray();
        }
        context.LatestTracker()!.Scene!.CharactersPresent = context.LatestTracker()!.Scene!.CharactersPresent.Distinct().ToArray();
        
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

                             {PromptSections.McStorySummary(context)}
                             """;
        chatHistory.AddUserMessage($"""
                                    {PromptSections.CharacterForEmulation(context)}

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
                             {PromptSections.ExtraLoreEntries(context.ExtraLoreEntries)}

                             {PromptSections.InitialInstruction(instruction.FirstSceneGuidance)}

                             Generate a detailed scene based on the above resolution and context.
                             """;
        }
        else
        {
            var incomingDispatches = await GetIncomingDispatchesAsync(context, _dispatchService, cancellationToken);
            requestPrompt = $"""
                             {incomingDispatches}

                             {PromptSections.PlayerAction(context.PlayerAction)}

                             ---
                             Ensure the output is wrapped in correct XML tags. Remember about the <scene> tag!
                             ## Quick Reference
                             
                             **Emulation:**
                             - Call for EVERY full-profile character ON THE SCENE, EVERY beat
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
        var callerContext = new CallerContext(GetType().Name, context.AdventureId, context.NewSceneId);
        await _pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await _pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        await _pluginFactory.AddPluginAsync<OrchestrateEmulationPlugin>(kernel, context, callerContext);
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

        // Queue MC dispatches for persistence
        if (newScene.Dispatches is { Count: > 0 })
        {
            foreach (var dispatch in newScene.Dispatches)
            {
                context.NewDispatches.Add(new DispatchToSave
                {
                    AdventureId = context.AdventureId,
                    FromCharacter = context.MainCharacter.Name,
                    ToCharacter = dispatch.To,
                    Method = dispatch.Method,
                    SentAt = dispatch.SentAt,
                    EstimatedTransit = dispatch.EstimatedTransit,
                    SenderContext = dispatch.SenderContext,
                    WhatArrives = dispatch.WhatArrives
                });
            }
        }

        // Queue MC dispatch resolutions for persistence
        if (newScene.DispatchesResolved is { Count: > 0 })
        {
            foreach (var resolution in newScene.DispatchesResolved)
            {
                if (Guid.TryParse(resolution.DispatchId, out var dispatchId))
                {
                    context.DispatchResolutions.Add(new DispatchResolutionToSave
                    {
                        DispatchId = dispatchId,
                        Resolution = resolution.Resolution,
                        ResolvedAt = resolution.Time,
                        Discoverable = resolution.Discoverable,
                        Location = context.NewTracker?.Scene?.Location
                    });

                    if (resolution.Discoverable)
                    {
                        context.NewWorldEvents.Add(new WorldEvent
                        {
                            When = resolution.Time,
                            Where = context.NewTracker?.Scene?.Location ?? "Unknown",
                            Event = resolution.Resolution
                        });
                    }
                }
            }
        }
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

            List<OutgoingDispatch>? dispatches = null;
            List<DispatchResolution>? dispatchesResolved = null;

            if (response.Contains("dispatch"))
            {
                var dispatchBlock = ResponseParser.TryExtractJson<DispatchOutput?>(response, "dispatch");
                if (dispatchBlock != null)
                {
                    dispatches = dispatchBlock.Dispatches;
                    dispatchesResolved = dispatchBlock.DispatchesResolved;
                }
                else
                {
                    dispatches = ResponseParser.TryExtractJson<List<OutgoingDispatch>?>(response, "dispatches");
                    dispatchesResolved = ResponseParser.TryExtractJson<List<DispatchResolution>?>(response, "dispatches_resolved");
                }
            }

            return new GeneratedScene
            {
                Scene = scene,
                Choices = choices,
                CreationRequests = creationRequests,
                ImportanceFlags = importanceFlags,
                Dispatches = dispatches,
                DispatchesResolved = dispatchesResolved
            };
        };
    }

    /// <summary>
    ///     Builds a prompt section for incoming dispatches that have arrived for the MC.
    /// </summary>
    private async Task<string> GetIncomingDispatchesAsync(
        GenerationContext context,
        DispatchService dispatchService,
        CancellationToken ct)
    {
        var dispatches = await dispatchService.GetDeliverableForRecipientAsync(
            context.AdventureId,
            context.MainCharacter.Name,
            ct);

        if (dispatches.Count == 0)
        {
            return string.Empty;
        }

        var incoming = dispatches.Select(d => new Models.IncomingDispatch
        {
            Id = d.Id.ToString(),
            From = d.FromCharacter,
            Method = d.Method,
            SentAt = d.SentAt,
            EstimatedTransit = d.EstimatedTransit,
            WhatArrives = d.WhatArrives
        }).ToList();

        var json = System.Text.Json.JsonSerializer.Serialize(incoming, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        return $"""
                <incoming_dispatches>
                Messages that have arrived for the MC:
                {json}
                </incoming_dispatches>
                """;
    }
}