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
    private Dictionary<string, ChatHistory> _chatHistory = new();
    private GenerationContext _generationContext = null!;
    private IKernelBuilder _kernelBuilder = null!;
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
        var promptTemplate = await BuildInstruction(generationContext);
        var characters = new List<CharacterContext>(generationContext.Characters);
        characters.AddRange(generationContext.NewCharacters ?? Array.Empty<CharacterContext>());
        _chatHistory = characters.ToDictionary(character => character.Name,
            context =>
            {
                var systemPrompt = promptTemplate.Replace(PlaceholderNames.CharacterName, context.Name);
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(BuildContextMessage(context, generationContext));
                return chatHistory;
            });
    }

    private string BuildContextMessage(CharacterContext context, GenerationContext generationContext)
    {
        var previousScenes = _generationContext
            .SceneContext
            .Where(x => x.Characters.Select(y => y.Name).Contains(context.Name) == true)
            .OrderByDescending(x => x.SequenceNumber)
            .TakeLast(20)
            .Select(s => $"""
                          SCENE NUMBER: {s.SequenceNumber}
                          <scene_tracker>
                          {s.Metadata?.Tracker?.Story.ToJsonString()}
                          </scene_tracker>

                          {s.SceneContent}
                          {s.PlayerChoice}
                          """);

        var latestScene = _generationContext.SceneContext.MaxBy(x => x.SequenceNumber);

        return $"""
                <character_description>
                {context.Description}
                </character_description>

                <character_state>
                {context.CharacterState.ToJsonString()}
                </character_state>

                <character_tracker>
                {context.CharacterTracker.ToJsonString()}
                </character_tracker>

                <character_development_tracker>
                {context.DevelopmentTracker.ToJsonString()}
                </character_development_tracker>

                <context>
                {_generationContext.ContextGathered.ToJsonString()}
                </context>

                CRITICAL! These scenes are written in the perspective of story protagonist - {generationContext.MainCharacter.Name}!
                <previous_scenes_with_character>
                {string.Join("\n\n---\n\n", previousScenes)}
                </previous_scenes_with_character>

                <current_time>
                {latestScene?.Metadata.Tracker?.Story?.Time}
                </current_time>
                <current_location>
                {latestScene?.Metadata.Tracker?.Story?.Location}
                </current_location>
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
        if (!_chatHistory.TryGetValue(characterName, out ChatHistory? chatHistory))
        {
            _logger.Information("Character {CharacterName} not found in chat history. Current chatHistory: {history}", characterName, _chatHistory.Keys);
            return "Character not found.";
        }

        Microsoft.SemanticKernel.IKernelBuilder kernel = _kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(_ragSearch, new CallerContext(GetType(), _generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
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