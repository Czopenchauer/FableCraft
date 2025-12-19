using System.ComponentModel;

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Plugins;

internal sealed class CharacterPlugin : BaseAgent
{
    private const int MemoryLimit = 20;

    private Dictionary<string, (CharacterContext, ChatHistory)> _chatHistory = new();
    private GenerationContext _generationContext = null!;
    private IKernelBuilder _kernelBuilder = null!;
    private int _currentSequenceNumber;
    private readonly IAgentKernel _agentKernel;
    private readonly ILogger _logger;
    private readonly IRagSearch _ragSearch;

    public CharacterPlugin(IAgentKernel agentKernel,
        ILogger logger,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        IRagSearch ragSearch) : base(dbContextFactory, kernelBuilderFactory)
    {
        _agentKernel = agentKernel;
        _logger = logger;
        _ragSearch = ragSearch;
    }

    public async Task Setup(GenerationContext generationContext)
    {
        _generationContext = generationContext;
        _kernelBuilder = await GetKernelBuilder(generationContext);
        _currentSequenceNumber = generationContext.SceneContext.Max(x => x.SequenceNumber);

        var characters = new List<CharacterContext>(generationContext.Characters);
        characters.AddRange(generationContext.NewCharacters ?? Array.Empty<CharacterContext>());

        var promptTemplate = await BuildInstruction(generationContext);
        _chatHistory = characters.ToDictionary(character => character.Name,
            context =>
            {
                var systemPrompt = promptTemplate.Replace(PlaceholderNames.CharacterName, context.Name);
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(BuildContextMessage(context, generationContext));
                return (context, chatHistory);
            });
    }

    private string BuildContextMessage(CharacterContext context, GenerationContext generationContext)
    {
        var previousScenes = context.SceneRewrites
            .OrderByDescending(x => x.SequenceNumber)
            .TakeLast(20)
            .Select(s => $"""
                          SCENE NUMBER: {s.SequenceNumber}
                          <scene_tracker>
                          TIME: {s.StoryTracker.Time}
                          Location: {s.StoryTracker.Weather}
                          Weather: {s.StoryTracker.Location}
                          Characters on scene: {string.Join(", ", s.StoryTracker.CharactersPresent)}
                          </scene_tracker>

                          {s.Content}
                          """);

        var latestScene = _generationContext.SceneContext.MaxBy(x => x.SequenceNumber);

        var memoriesSection = BuildMemoriesSection(context);

        var relationshipsSection = BuildRelationshipsSection(context, generationContext);

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
                {latestScene?.Metadata.Tracker?.Story?.Time}
                </current_time>
                <current_location>
                {latestScene?.Metadata.Tracker?.Story?.Location}
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
            context.CharacterMemories.Select(m => $"- [Time: {m.StoryTracker.Time}, Location: {m.StoryTracker.Location}] {m.Summary} [{m.Data}]"));

        return $"""
                <character_memories>
                These are the {context.Name}'s memories from past scenes (ordered by recency):
                {memoriesText}
                </character_memories>
                """;
    }

    private string BuildRelationshipsSection(CharacterContext context, GenerationContext generationContext)
    {
        var relationshipsText = string.Join("\n\n",
            context.Relationships.Select(r => $"""
                                               **{r.TargetCharacterName}**:
                                               {generationContext.Characters.Single(x => x.Name == r.TargetCharacterName).Description}
                                               """));

        return $"""
                <character_relationships>
                The character's has relationship with these characters:
                {relationshipsText}
                </character_relationships>
                """;
    }

    [KernelFunction("emulate_character_action")]
    [Description(
        "Emulate a character's action based on the character's personality, motivations, and the current situation. Use this to generate character dialogue, actions, reactions, or decisions that are consistent with their established traits and the narrative context.")]
    public async Task<string> EmulateCharacterAction(
        [Description("The current situation or context in which the character is acting")]
        string situation,
        [Description("The name of the character whose action is to be emulated. Use exactly the same name as defined in the character context.")]
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

        var kgPlugin = new CharacterGraphPlugin(_ragSearch, new CallerContext(GetType(), _generationContext.AdventureId), characterContext, _logger);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        var relationShipPlugin = new CharacterStatePlugin(characterContext, _logger);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(relationShipPlugin));

        Kernel kernelWithKg = kernel.Build();
        var chat = new ChatHistory();
        foreach (ChatMessageContent chatMessageContent in chatHistory)
        {
            chat.Add(chatMessageContent);
        }

        chat.AddUserMessage(situation);
        var outputFunc = new Func<string, string>(response => response);
        var response = await _agentKernel.SendRequestAsync(chat,
            outputFunc,
            _kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            $"{nameof(CharacterPlugin)}:{characterName}",
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