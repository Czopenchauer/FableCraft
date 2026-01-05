using System.Text.Json;
using System.Text.Json.Serialization;

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

internal sealed class CharacterCrafter : BaseAgent
{
    private readonly IAgentKernel _agentKernel;
    private readonly IPluginFactory _pluginFactory;

    public CharacterCrafter(
        IAgentKernel agentKernel,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IPluginFactory pluginFactory) : base(contextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _pluginFactory = pluginFactory;
    }

    public async Task<CharacterContext> Invoke(
        GenerationContext context,
        CharacterRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);
        await using ApplicationDbContext dbContext = await DbContextFactory.CreateDbContextAsync(cancellationToken);

        var systemPrompt = await BuildInstruction(context);
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var promptContext = $"""
                             {PromptSections.WorldSettings(context.WorldSettings)}

                             {PromptSections.Context(context)}

                             {PromptSections.LastScenes(context.SceneContext, 3)}
                             """;
        chatHistory.AddUserMessage(promptContext);
        var creationRequestPrompt = $"""
                                     {PromptSections.CurrentScene(context)}

                                     Here is the character creation request for you to process:
                                     {PromptSections.CharacterCreationContext(request)}
                                     """;
        chatHistory.AddUserMessage(creationRequestPrompt);
        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);
        await _pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        Kernel kernelWithKg = kernel.Build();

        var outputParser = CreateOutputParser();

        (CharacterStats characterStats, string description, CharacterTracker tracker, InitialRelationship[] relationships) result =
            await _agentKernel.SendRequestAsync(
                chatHistory,
                outputParser,
                kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
                nameof(CharacterCrafter),
                kernelWithKg,
                cancellationToken);

        return new CharacterContext
        {
            CharacterId = Guid.NewGuid(),
            CharacterState = result.characterStats,
            Description = result.description,
            CharacterTracker = result.tracker,
            Name = result.characterStats.CharacterIdentity.FullName!,
            CharacterMemories = new List<MemoryContext>(),
            Relationships = result.relationships.Select(r => new CharacterRelationshipContext
                {
                    Data = r.ExtensionData,
                    TargetCharacterName = r.Name,
                    StoryTracker = null,
                    SequenceNumber = 0,
                    Dynamic = r.Dynamic
                })
                .ToList(),
            SceneRewrites = new List<CharacterSceneContext>(),
            Importance = request.Importance
        };
    }

    private static Func<string, (CharacterStats characterStats, string description, CharacterTracker tracker, InitialRelationship[] initialRelationships)>
        CreateOutputParser()
    {
        return response =>
        {
            var characterStats = ResponseParser.ExtractJson<CharacterStats>(response, "character");
            var tracker = ResponseParser.ExtractJson<CharacterTracker>(response, "character_statistics");
            var relationship = ResponseParser.ExtractJson<InitialRelationship[]>(response, "initial_relationships");

            var description = ResponseParser.ExtractText(response, "character_description");

            if (string.IsNullOrEmpty(description))
            {
                throw new InvalidCastException("Failed to parse character description from response due to empty description.");
            }

            return (characterStats, description, tracker, relationship);
        };
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        JsonSerializerOptions options = PromptSections.GetJsonOptions();
        var structure = context.TrackerStructure;
        var prompt = await GetPromptAsync(context);
        var progressionSystem = await File.ReadAllTextAsync(Path.Combine(context.PromptPath, "ProgressionSystem.md"));
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.CharacterTrackerStructure, JsonSerializer.Serialize(GetSystemPrompt(structure), options)),
            (PlaceholderNames.CharacterTrackerOutput, JsonSerializer.Serialize(GetOutputJson(structure), options)),
            ("{{progression_system}}", progressionSystem));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToOutputJson(structure.Characters);
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToSystemJson(structure.Characters);
    }

    protected override AgentName GetAgentName() => AgentName.CharacterCrafter;

    private class InitialRelationship
    {
        public required string Name { get; set; }

        public required object Dynamic { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> ExtensionData { get; set; } = new();
    }
}