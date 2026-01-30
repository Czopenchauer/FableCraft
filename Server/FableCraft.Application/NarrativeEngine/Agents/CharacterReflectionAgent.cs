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

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
///     Character Reflection Agent - runs post-scene for each meaningful character present.
///     Output:
///     - scene_rewrite: Full character-POV prose -> stored in KG
///     - memory: Summary, salience, entities, emotional_tone -> stored in DB
///     - relationship_updates: Per-character relationship state -> stored in DB
///     - psychology, motivations, in_development -> stored in CharacterStats
/// </summary>
internal sealed class CharacterReflectionAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    ILogger logger,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.CharacterReflectionAgent;

    public async Task<CharacterContext> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        SceneTracker sceneTrackerResult,
        CancellationToken cancellationToken)
    {
        var kernelBuilder = await GetKernelBuilder(generationContext);

        var systemPrompt = await BuildInstruction(generationContext, context, sceneTrackerResult);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var relationshipsText = string.Join("\n\n",
            context.Relationships.Select(r => $"""
                                               **{r.TargetCharacterName}**:
                                               {context.Relationships.Single(x => x.TargetCharacterName == r.TargetCharacterName).Data.ToJsonString()}
                                               """));

        var relationship = $"""
                            <character_relationships>
                            The character's has relationship with these characters:
                            {relationshipsText}
                            </character_relationships>
                            """;
        var contextPrompt = $"""
                             Always use exact names for characters!
                             {PromptSections.ExistingCharacters(generationContext.Characters)}

                             {relationship}

                             {PromptSections.MainCharacter(generationContext)}

                             {PromptSections.WorldSettings(generationContext.PromptPath)}

                             {PromptSections.NewItems(generationContext.NewItems)}

                             {PromptSections.RecentScenesForCharacter(context)}

                             {PromptSections.CharacterEmulationOutputs(generationContext, context.Name)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.CharacterStateContext(context)}

                             {PromptSections.SceneTracker(generationContext, sceneTrackerResult)}

                             {PromptSections.CurrentScene(generationContext)}

                             Reflect on this scene from {context.Name}'s perspective. Generate your output with only the sections that changed.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterReflectionOutput>("character_reflection", true);

        var kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), generationContext.AdventureId, generationContext.NewSceneId);
        await pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, generationContext, callerContext, context.CharacterId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, generationContext, callerContext);
        await pluginFactory.AddCharacterPluginAsync<CharacterRelationshipPlugin>(kernel, generationContext, callerContext, context.CharacterId);
        var kernelWithKg = kernel.Build();

        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var output = await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(CharacterReflectionAgent),
            kernelWithKg,
            cancellationToken);

        var characterState = context.CharacterState;

        if (output.ProfileUpdates != null)
        {
            characterState = output.ProfileUpdates;
        }
        else
        {
            logger.Warning("Character reflection output for character {CharacterName} has no update for character state.", context.Name);
        }

        var characterRelationships = new List<CharacterRelationshipContext>();
        foreach (var reflectionOutputRelationshipUpdate in output.RelationshipUpdates)
        {
            if (reflectionOutputRelationshipUpdate.ExtensionData?.Count == 0)
            {
                logger.Warning("Character {CharacterName} has no relationships update!", context.Name);
            }

            var matchedRelationship =
                context.Relationships.SingleOrDefault(x => x.TargetCharacterName == reflectionOutputRelationshipUpdate.Toward);
            if (matchedRelationship == null)
            {
                characterRelationships.Add(new CharacterRelationshipContext
                {
                    TargetCharacterName = reflectionOutputRelationshipUpdate.Toward,
                    Data = reflectionOutputRelationshipUpdate.ExtensionData!,
                    UpdateTime = sceneTrackerResult.Time,
                    SequenceNumber = 0,
                    Dynamic = reflectionOutputRelationshipUpdate.Dynamic!
                });
            }
            else if (reflectionOutputRelationshipUpdate.ExtensionData?.Count > 0)
            {
                var newRelationship = new CharacterRelationshipContext
                {
                    TargetCharacterName = matchedRelationship.TargetCharacterName,
                    Data = reflectionOutputRelationshipUpdate.ExtensionData!,
                    UpdateTime = sceneTrackerResult.Time,
                    SequenceNumber = matchedRelationship.SequenceNumber + 1,
                    Dynamic = reflectionOutputRelationshipUpdate.Dynamic ?? matchedRelationship.Dynamic
                };
                characterRelationships.Add(newRelationship);
            }
        }

        var memory = new List<MemoryContext>();
        if (output.Memory is not null)
        {
            memory.Add(new MemoryContext
            {
                Salience = output.Memory!.Salience,
                Data = output.Memory.ExtensionData!,
                MemoryContent = output.Memory.Summary,
                SceneTracker = sceneTrackerResult
            });
        }

        return new CharacterContext
        {
            CharacterId = context.CharacterId,
            CharacterState = characterState,
            CharacterTracker = context.CharacterTracker,
            Name = context.Name,
            Description = context.Description,
            CharacterMemories = memory,
            Relationships = characterRelationships,
            SceneRewrites =
            [
                new CharacterSceneContext
                {
                    Content = output.SceneRewrite,
                    SceneTracker = sceneTrackerResult,
                    SequenceNumber = context.SceneRewrites.MaxBy(x => x.SequenceNumber)
                                         ?.SequenceNumber
                                     + 1
                                     ?? 0
                }
            ],
            Importance = context.Importance,
            SimulationMetadata = null,
            IsDead = false
        };
    }

    private async Task<string> BuildInstruction(
        GenerationContext generationContext,
        CharacterContext characterContext,
        SceneTracker sceneTrackerResult)
    {
        var prompt = await GetPromptAsync(generationContext);
        var jsonOptions = PromptSections.GetJsonOptions(true);

        prompt = prompt.Replace(PlaceholderNames.CharacterName, characterContext.Name);

        prompt = prompt.Replace("{{character_identity}}", characterContext.CharacterState.ToJsonString(jsonOptions));

        prompt = prompt.Replace("{{character_tracker}}",
            characterContext.CharacterTracker?.ToJsonString(jsonOptions) ?? "No physical state tracked.");

        var charactersPresent = sceneTrackerResult.CharactersPresent ?? [];
        var relationshipsOnScene = FormatRelationshipsOnScene(characterContext, charactersPresent, jsonOptions);
        prompt = prompt.Replace("{{relationships_on_scene}}", relationshipsOnScene);

        return prompt;
    }

    private static string FormatRelationshipsOnScene(
        CharacterContext characterContext,
        string[] charactersPresent,
        System.Text.Json.JsonSerializerOptions jsonOptions)
    {
        if (characterContext.Relationships.Count == 0 || charactersPresent.Length == 0)
        {
            return "No established relationships with characters present.";
        }

        var presentSet = new HashSet<string>(charactersPresent, StringComparer.OrdinalIgnoreCase);
        var relevantRelationships = characterContext.Relationships
            .Where(r => presentSet.Contains(r.TargetCharacterName))
            .ToList();

        if (relevantRelationships.Count == 0)
        {
            return "No established relationships with characters present.";
        }

        var sb = new System.Text.StringBuilder();
        foreach (var rel in relevantRelationships)
        {
            sb.AppendLine($"### {rel.TargetCharacterName}");
            sb.AppendLine($"**Dynamic:** {rel.Dynamic}");
            if (rel.Data.Count > 0)
            {
                sb.AppendLine($"**Details:** {rel.Data.ToJsonString(jsonOptions)}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}