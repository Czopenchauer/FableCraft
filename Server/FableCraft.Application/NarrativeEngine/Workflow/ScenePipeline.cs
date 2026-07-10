using FableCraft.Application.NarrativeEngine.Agents;
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

namespace FableCraft.Application.NarrativeEngine.Workflow;

internal sealed class ScenePipeline(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory,
    DispatchService dispatchService,
    QualityAssuranceAgent qualityAssuranceAgent,
    NarrativeCatalystAgent narrativeCatalystAgent,
    Serilog.ILogger logger) : IProcessor
{
    public async Task Invoke(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.NewScene is not null)
        {
            return;
        }

        await RunNarrativeCatalystPass(context, cancellationToken);
        await RunDraftPass(context, cancellationToken);

        if (context.NewScene is null || string.IsNullOrWhiteSpace(context.NewScene.Scene))
        {
            throw new InvalidOperationException("ScenePipeline: Writer produced empty scene output.");
        }

        var draftScene = context.NewScene;
        var qaReview = await RunQaPass(context, cancellationToken);

        // if (qaReview.IsPass)
        // {
        //     logger.Information("ScenePipeline: QA verdict PASS for adventure {AdventureId}", context.AdventureId);
        //     return;
        // }

        logger.Information("ScenePipeline: QA verdict REVISE for adventure {AdventureId}", context.AdventureId);
        context.ScenePipelineRevisionComplete = true;

        var revisedScene = await RunRevisionPass(context, draftScene, qaReview, cancellationToken);

        if (revisedScene is null || string.IsNullOrWhiteSpace(revisedScene.Scene))
        {
            logger.Warning("ScenePipeline: Revision produced empty output for adventure {AdventureId}, serving draft",
                context.AdventureId);
            context.NewScene = draftScene;
            return;
        }

        context.NewScene = revisedScene;
        logger.Information("ScenePipeline: Revision completed for adventure {AdventureId}", context.AdventureId);
    }

    private async Task RunNarrativeCatalystPass(GenerationContext context, CancellationToken cancellationToken)
    {
        if (context.NarrativeCatalystOutput is not null)
        {
            return;
        }

        var sceneTracker = context.LatestTracker()?.Scene;
        if (sceneTracker is null)
        {
            logger.Information("ScenePipeline: Skipping NarrativeCatalyst (no previous scene tracker available)");
            return;
        }

        var sceneTrackerResult = context.NewTracker?.Scene ?? sceneTracker;
        await narrativeCatalystAgent.Invoke(context, sceneTrackerResult, cancellationToken);
        logger.Information("ScenePipeline: NarrativeCatalyst completed for adventure {AdventureId}", context.AdventureId);
    }

    private async Task RunDraftPass(GenerationContext context, CancellationToken cancellationToken)
    {
        context.CharacterEmulationOutputs.Clear();
        foreach (GatheredCoLocatedCharacter gatheredContextCoLocatedCharacter in context.SceneContext
                                                                                     .OrderByDescending(x => x.SequenceNumber)
                                                                                     .FirstOrDefault()?.Metadata?.GatheredContext?.CoLocatedCharacters
                                                                                 ?? [])
        {
            context.LatestTracker()!.Scene!.CharactersPresent = context.LatestTracker()!.Scene!.CharactersPresent
                .Append(gatheredContextCoLocatedCharacter.Name).ToArray();
        }

        context.LatestTracker()?.Scene?.CharactersPresent = context.LatestTracker()?.Scene?.CharactersPresent
                                                                .Distinct().ToArray()
                                                            ?? [];

        var kernelBuilder = await CreateKernelBuilder(context, AgentName.WriterAgent);
        var systemPrompt = await LoadPromptAsync(context, AgentName.WriterAgent);
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
                     .Take(WriterSceneContextCount)
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
        bool requireSimulation = context.Characters.Select(x => x.Name)
            .Intersect(context.LatestTracker()?.Scene?.CharactersPresent ?? []).Any();
        if (!hasSceneContext)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var instruction = await dbContext.Adventures
                .Select(x => new
                {
                    x.Id,
                    x.FirstSceneGuidance
                })
                .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);
            requestPrompt = $"""
                             {PromptSections.InitialInstruction(instruction.FirstSceneGuidance)}

                             Generate a detailed scene based on the above resolution and context.
                             """;
        }
        else
        {
            var incomingDispatches = await GetIncomingDispatchesAsync(context, cancellationToken);
            requestPrompt = $"""
                             {PromptSections.NarrativeCatalystGuidance(context)}

                             {incomingDispatches}

                             {PromptSections.PlayerAction(context.PlayerAction)}

                             ---
                             Ensure the output is wrapped in correct XML tags. Remember about the <scene> tag!
                             ## Quick Reference

                             Keep the scene short. DO NOT MAKE IT DRAMATIC. CHARACTERS ARE NOT NARRATING their action and parroting what was already said. Repetition is prohibited. Always try to push action and story forward. The scene should not stall - something new should happen.

                             **MC Agency:**
                             - MC does ONLY what player input specified—nothing more
                             - No invented dialogue, decisions, or "helpful" additional actions
                             - Wishful thinking ("I convince," "knowing this will earn trust") = inner monologue, not world effect
                             - No mechanism = MC acts, world doesn't bend

                             You are prohibited of making gamer, analytical, strategic bullshit. MC is a human being - write them as such.

                             **Never invent. Always ask.**
                             Generate a detailed scene based on the above resolution and context.
                             """;
        }

        chatHistory.AddUserMessage(requestPrompt);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(nameof(ScenePipeline), context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);
        if (requireSimulation)
        {
            await pluginFactory.AddPluginAsync<CharacterEmulationPlugin>(kernel, context, callerContext);
        }

        var kernelWithKg = kernel.Build();
        var outputParser = CreateSceneOutputParser();

        var newScene = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(ScenePipeline),
            kernelWithKg,
            cancellationToken,
            new AgentKernelOptions { MaxParsingRetries = 1 });

        context.NewScene = newScene;
        SaveDispatches(context, newScene);
    }

    private async Task<QaReviewOutput> RunQaPass(GenerationContext context, CancellationToken cancellationToken)
    {
        var review = await qualityAssuranceAgent.Invoke(context, cancellationToken);
        context.QaReview = review;
        return review;
    }

    private async Task<GeneratedScene?> RunRevisionPass(
        GenerationContext context,
        GeneratedScene draftScene,
        QaReviewOutput qaReview,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await CreateKernelBuilder(context, AgentName.WriterAgent);
        var systemPrompt = await LoadPromptAsync(context, AgentName.WriterAgent);
        systemPrompt = systemPrompt.Replace(PlaceholderNames.CharacterName, context.MainCharacter.Name);

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
                     .Take(WriterSceneContextCount)
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

        var revisionMessage = BuildRevisionMessage(draftScene, qaReview);
        chatHistory.AddUserMessage(revisionMessage);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(nameof(ScenePipeline), context.AdventureId, context.NewSceneId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        await pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, context, callerContext);

        var requireSimulation = context.Characters.Select(x => x.Name)
            .Intersect(context.LatestTracker()?.Scene?.CharactersPresent ?? []).Any();
        if (requireSimulation)
        {
            await pluginFactory.AddPluginAsync<CharacterEmulationPlugin>(kernel, context, callerContext);
        }

        var kernelWithKg = kernel.Build();
        var outputParser = CreateSceneOutputParser();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            $"{nameof(ScenePipeline)}_Revision",
            kernelWithKg,
            cancellationToken,
            new AgentKernelOptions { MaxParsingRetries = 1 });
    }

    private static string BuildRevisionMessage(GeneratedScene draftScene, QaReviewOutput qaReview)
    {
        var choicesText = draftScene.Choices.Length > 0
            ? string.Join("\n", draftScene.Choices.Select((c, i) => $"{i + 1}. {c}"))
            : "(No choices in draft)";

        return $"""
                <revision_request>
                You previously wrote the following scene. The Quality Assurance agent has flagged issues that need to be fixed.

                <draft_scene>
                {draftScene.Scene}
                </draft_scene>

                <draft_choices>
                {choicesText}
                </draft_choices>

                <qa_feedback>
                {qaReview.ReviewText}
                </qa_feedback>

                Fix every issue flagged in the QA review. Address each issue at the exact location specified. Apply the fix directions from the review — not your own ideas about what should change. Preserve everything that was not flagged — do not rewrite from scratch. Do not add new content, subplots, or characters that weren't in the draft. Do not extend the scene beyond its original endpoint. Produce a complete revised scene, not patches.

                Produce your output in the same format: <scene> followed by <choices>.
                </revision_request>
                """;
    }

    private async Task<IKernelBuilder> CreateKernelBuilder(GenerationContext context, AgentName agentName)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var agentLlmPreset = context.AgentLlmPreset.Single(x => x.AgentName == agentName);
        return kernelBuilderFactory.Create(agentLlmPreset.LlmPreset);
    }

    private async Task<string> LoadPromptAsync(GenerationContext context, AgentName agentName)
    {
        var agentPromptPath = Path.Combine(context.PromptPath, $"{agentName}.md");
        var promptTemplate = await File.ReadAllTextAsync(agentPromptPath);
        var prompt = await ReplaceContentPolicyPlaceholder(promptTemplate, context.PromptPath);
        var storyBible = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "StoryBible.md"));
        var progressionSystem = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "ProgressionSystem.md"));
        var identitySchema = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "IdentitySchema.md"));
        var relationshipSchema = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "RelationshipSchema.md"));
        var dispatch = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "Dispatch.md"));
        var worldSettingsPath = Path.Combine(context.PromptPath, "WorldSettings.md");
        var worldSettings = File.Exists(worldSettingsPath) ? await File.ReadAllTextAsync(worldSettingsPath) : string.Empty;
        return prompt
            .Replace(PlaceholderNames.StoryBible, storyBible)
            .Replace(PlaceholderNames.ProgressionSystem, progressionSystem)
            .Replace(PlaceholderNames.RelationshipSchema, relationshipSchema)
            .Replace(PlaceholderNames.IdentitySchema, identitySchema)
            .Replace(PlaceholderNames.Dispatch, dispatch)
            .Replace(PlaceholderNames.WorldSetting, worldSettings);
    }

    private static async Task<string> ReplaceContentPolicyPlaceholder(string promptTemplate, string promptPath)
    {
        if (!promptTemplate.Contains(PlaceholderNames.ContentPolicy))
        {
            return promptTemplate;
        }

        var filePath = Path.Combine(promptPath, "ContentPolicy.md");
        if (File.Exists(filePath))
        {
            var fileContent = await File.ReadAllTextAsync(filePath);
            return promptTemplate.Replace(PlaceholderNames.ContentPolicy, fileContent);
        }

        return promptTemplate;
    }

    private async Task<string> GetIncomingDispatchesAsync(GenerationContext context, CancellationToken ct)
    {
        var dispatches = await dispatchService.GetDeliverableForRecipientAsync(
            context.AdventureId,
            context.MainCharacter.Name,
            ct);

        if (dispatches.Count == 0)
        {
            return string.Empty;
        }

        var incoming = dispatches.Select(d => new IncomingDispatch
        {
            Id = d.Id.ToString(),
            From = d.FromCharacter,
            Method = d.Method,
            SentAt = d.SentAt,
            EstimatedTransit = d.EstimatedTransit,
            WhatArrives = d.WhatArrives
        }).ToList();

        var json = System.Text.Json.JsonSerializer.Serialize(incoming,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        return $"""
                <incoming_dispatches>
                Messages that have arrived for the MC:
                {json}
                </incoming_dispatches>
                """;
    }

    private static void SaveDispatches(GenerationContext context, GeneratedScene newScene)
    {
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

    private static Func<string, GeneratedScene> CreateSceneOutputParser()
    {
        return response =>
        {
            var scene = ResponseParser.ExtractText(response, "scene");
            var choicesText = ResponseParser.ExtractText(response, "choices");
            var choices = choicesText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
                CreationRequests = null,
                ImportanceFlags = null,
                Dispatches = dispatches,
                DispatchesResolved = dispatchesResolved
            };
        };
    }

    private const int WriterSceneContextCount = 10;
}