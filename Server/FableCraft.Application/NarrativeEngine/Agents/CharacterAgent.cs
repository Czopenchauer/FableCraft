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
/// Agent that emulates character actions based on personality, motivations, and context.
/// Manages per-character chat histories and LLM interactions.
/// </summary>
internal sealed class CharacterAgent : BaseAgent
{
    private Dictionary<string, (CharacterContext, ChatHistory)> _chatHistory = new();
    private GenerationContext _generationContext = null!;
    private IKernelBuilder _kernelBuilder = null!;
    private readonly IAgentKernel _agentKernel;
    private readonly ILogger _logger;
    private readonly IPluginFactory _pluginFactory;

    public CharacterAgent(IAgentKernel agentKernel,
        ILogger logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IPluginFactory pluginFactory) : base(dbContextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _logger = logger;
        _pluginFactory = pluginFactory;
    }

    public async Task Setup(GenerationContext generationContext)
    {
        _generationContext = generationContext;
        _kernelBuilder = await GetKernelBuilder(generationContext);

        var characters = new List<CharacterContext>(generationContext.Characters);
        characters.AddRange(generationContext.NewCharacters ?? Array.Empty<CharacterContext>());

        var promptTemplate = await BuildInstruction(generationContext);
        _chatHistory = characters.ToDictionary(character => character.Name,
            context =>
            {
                var systemPrompt = promptTemplate.Replace(PlaceholderNames.CharacterName, context.Name);
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(BuildContextMessage(context));
                return (context, chatHistory);
            });
    }

    private string BuildContextMessage(CharacterContext context)
    {
        var previousScenes = context.SceneRewrites
            .OrderByDescending(x => x.SequenceNumber)
            .Skip(1)
            .Take(20)
            .OrderBy(x => x.SequenceNumber)
            .Select(s => $"""
                          SCENE NUMBER: {s.SequenceNumber}
                          <scene_tracker>
                          TIME: {s.StoryTracker?.Time}
                          Location: {s.StoryTracker?.Weather}
                          Weather: {s.StoryTracker?.Location}
                          Characters on scene: {string.Join(", ", s.StoryTracker?.CharactersPresent ?? [])}
                          </scene_tracker>

                          {s.Content}
                          """);

        var latestScene = _generationContext.SceneContext.MaxBy(x => x.SequenceNumber);

        var memoriesSection = BuildMemoriesSection(context);

        var relationshipsSection = BuildRelationshipsSection(context);

        return $"""
                <character_description>
                {context.Description}
                </character_description>

                {relationshipsSection}

                <character_state>
                {context.CharacterState.ToJsonString()}
                </character_state>

                <character_tracker>
                {context.CharacterTracker.ToJsonString()}
                </character_tracker>

                <previous_scenes>
                {string.Join("\n\n---\n\n", previousScenes)}
                </previous_scenes>

                {memoriesSection}

                <current_time>
                {latestScene?.Metadata.Tracker?.Scene?.Time}
                </current_time>
                <current_location>
                {latestScene?.Metadata.Tracker?.Scene?.Location}
                </current_location>
                """;
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

    private string BuildRelationshipsSection(CharacterContext context)
    {
        var relationshipsText = string.Join("\n\n",
            context.Relationships.Select(r => $"""
                                               **{r.TargetCharacterName}**:
                                               {context.Relationships.Single(x => x.TargetCharacterName == r.TargetCharacterName).Data.ToJsonString()}
                                               """));

        return $"""
                <character_relationships>
                The character's has relationship with these characters:
                {relationshipsText}
                </character_relationships>
                """;
    }

    public async Task<string> EmulateCharacterAction(
        string situation,
        string query,
        string characterName)
    {
        _logger.Information("Emulating action for character {CharacterName} in situation: {Situation}", characterName, situation);
        if (!_chatHistory.TryGetValue(characterName, out var ctx))
        {
            _logger.Information("Character {CharacterName} not found in chat history. Current chatHistory: {history}", characterName, _chatHistory.Keys);
            return "Character not found.";
        }

        (CharacterContext characterContext, ChatHistory chatHistory) = ctx;
        Microsoft.SemanticKernel.IKernelBuilder kernel = _kernelBuilder.Create();

        var callerContext = new CallerContext(GetType(), _generationContext.AdventureId);
        await _pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, _generationContext, callerContext, characterContext.CharacterId);
        await _pluginFactory.AddCharacterPluginAsync<CharacterRelationshipPlugin>(kernel, _generationContext, callerContext, characterContext.CharacterId);

        Kernel kernelWithKg = kernel.Build();
        var chat = new ChatHistory();
        foreach (ChatMessageContent chatMessageContent in chatHistory)
        {
            chat.Add(chatMessageContent);
        }

        chat.AddUserMessage(situation);
        chat.AddUserMessage(query);
        var outputFunc = new Func<string, string>(response => response);
        var response = await _agentKernel.SendRequestAsync(chat,
            outputFunc,
            _kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            $"{nameof(CharacterAgent)}:{characterName}",
            kernelWithKg,
            CancellationToken.None);
        _logger.Information("Received response for character {CharacterName}: {Response}", characterName, response);
        return response;
    }

    private async Task<string> BuildInstruction(GenerationContext context)
    {
        return await GetPromptAsync(context);
    }

    protected override AgentName GetAgentName() => AgentName.CharacterPlugin;
}