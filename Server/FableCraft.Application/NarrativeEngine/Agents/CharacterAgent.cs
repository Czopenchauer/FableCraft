using System.Text;

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
///     Agent that emulates character actions based on personality, motivations, and context.
///     Manages per-character chat histories and LLM interactions.
/// </summary>
internal sealed class CharacterAgent : BaseAgent
{
    private readonly IAgentKernel _agentKernel;
    private readonly ILogger _logger;
    private readonly IPluginFactory _pluginFactory;
    private Dictionary<string, (CharacterContext, ChatHistory)> _chatHistory = new();
    private GenerationContext _generationContext = null!;
    private IKernelBuilder _kernelBuilder = null!;

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

        List<CharacterContext> characters;
        lock (generationContext)
        {
            characters = new List<CharacterContext>(generationContext.Characters);
            characters.AddRange(generationContext.NewCharacters);
        }

        var promptTemplate = await BuildInstruction(generationContext);
        _chatHistory = characters.ToDictionary(character => character.Name,
            context =>
            {
                var systemPrompt = promptTemplate.Replace(PlaceholderNames.CharacterName, context.Name);
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                BuildContextMessage(context, chatHistory);
                return (context, chatHistory);
            });
    }

    private void BuildContextMessage(CharacterContext context, ChatHistory chatHistory)
    {
        var previousScenes = context.SceneRewrites
            .OrderByDescending(x => x.SequenceNumber)
            .Skip(1)
            .Take(30)
            .OrderBy(x => x.SequenceNumber)
            .Select(s => $"""
                          ----
                          SCENE NUMBER: {s.SequenceNumber}
                          <scene_tracker>
                          TIME: {s.SceneTracker?.Time}
                          Location: {s.SceneTracker?.Weather}
                          Weather: {s.SceneTracker?.Location}
                          Characters on scene: {string.Join(", ", s.SceneTracker?.CharactersPresent ?? [])}
                          </scene_tracker>

                          {s.Content}
                          ----

                          """);

        var latestScene = _generationContext.SceneContext.MaxBy(x => x.SequenceNumber);

        var memoriesSection = BuildMemoriesSection(context);

        var relationshipsSection = BuildRelationshipsSection(context);
        foreach (string previousScene in previousScenes)
        {
            chatHistory.AddUserMessage(previousScene);
        }

        chatHistory.AddUserMessage($"""
                                    <current_time>
                                    {latestScene?.Metadata.Tracker?.Scene?.Time}
                                    </current_time>
                                    <current_location>
                                    {latestScene?.Metadata.Tracker?.Scene?.Location}
                                    </current_location>
                                    """);

        var contextPrompt = $"""
                             {BuildMainCharacterSection(_generationContext, context)}

                             {memoriesSection}

                             {_generationContext.PreviouslyGeneratedLore}

                             <character_description>
                             {context.Description}
                             </character_description>

                             {relationshipsSection}

                             <character_tracker>
                             {context.CharacterTracker.ToJsonString()}
                             </character_tracker>

                             <character_state>
                             {context.CharacterState.ToJsonString()}
                             </character_state>
                             """;
        chatHistory.AddUserMessage(contextPrompt);
    }

    private async Task<string> BuildMemoriesSection(CharacterContext context)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        var memories = await dbContext.CharacterMemories.Where(x => x.CharacterId == context.CharacterId).ToArrayAsync();
        if (memories.Length == 0)
        {
            return string.Empty;
        }

        var memoriesText = string.Join("\n",
            memories.OrderBy(x => x.SceneTracker.Time).Select(m => $"- [Time: {m.SceneTracker.Time}, Location: {m.SceneTracker.Location}] {m.Summary} [{m.Data.ToJsonString()}]"));

        return $"""
                <character_memories>
                These are the {context.Name}'s memories from past scenes:
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

    private string BuildMainCharacterSection(GenerationContext context, CharacterContext currentCharacter)
    {
        var tracker = _generationContext.LatestTracker()?.MainCharacter?.MainCharacter
                      ?? context.NewTracker?.MainCharacter?.MainCharacter
                      ?? _generationContext.InitialMainCharacterTracker;
        var description = _generationContext.LatestTracker()?.MainCharacter?.MainCharacterDescription
                          ?? _generationContext.MainCharacter.Description;

        var name = tracker?.Name ?? _generationContext.MainCharacter.Name;
        var appearance = tracker?.Appearance ?? "Unknown";
        var generalBuild = tracker?.GeneralBuild ?? "Unknown";

        var builder = new StringBuilder($"""
                                         <character name="{name}">
                                         Appearance: {appearance}
                                         GeneralBuild: {generalBuild}
                                         {description}
                                         </character>
                                         """);
        var scene = context.LatestTracker()?.Scene?.CharactersPresent ?? [];
        var otherCharacters = _generationContext.Characters
            .Where(c => c.Name != currentCharacter.Name && scene.Contains(c.Name))
            .ToList();

        if (otherCharacters.Count == 0)
        {
            return builder.ToString();
        }

        otherCharacters.ForEach(c => builder.AppendLine($"""
                                                         <character name="{c.Name}">
                                                         Appearance: {c.CharacterTracker?.Appearance}
                                                         GeneralBuild: {c.CharacterTracker?.GeneralBuild}
                                                         {c.Description}
                                                         </character>
                                                         """));
        return builder.ToString();
    }

    public async Task<CharacterEmulationResult> EmulateCharacterAction(
        string situation,
        string query,
        string characterName)
    {
        _logger.Information("Emulating action for character {CharacterName} in situation: {Situation}", characterName, situation);
        if (!_chatHistory.TryGetValue(characterName, out var ctx))
        {
            _logger.Information("Character {CharacterName} not found in chat history. Current chatHistory: {history}", characterName, _chatHistory.Keys);
            return new CharacterEmulationResult("Character not found.", "Character not found.");
        }

        var (characterContext, chatHistory) = ctx;
        var kernel = _kernelBuilder.Create();

        var callerContext = new CallerContext($"{nameof(CharacterAgent)}:{characterName}", _generationContext.AdventureId, _generationContext.NewSceneId);
        await _pluginFactory.AddCharacterPluginAsync<CharacterNarrativePlugin>(kernel, _generationContext, callerContext, characterContext.CharacterId);
        await _pluginFactory.AddCharacterPluginAsync<CharacterRelationshipPlugin>(kernel, _generationContext, callerContext, characterContext.CharacterId);

        var kernelWithKg = kernel.Build();
        var chat = new ChatHistory();
        foreach (var chatMessageContent in chatHistory)
        {
            chat.Add(chatMessageContent);
        }

        chat.AddUserMessage(situation);
        chat.AddUserMessage(query);
        var outputParser = CreateOutputParser();
        var result = await _agentKernel.SendRequestAsync(chat,
            outputParser,
            _kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            $"{nameof(CharacterAgent)}:{characterName}",
            kernelWithKg,
            CancellationToken.None);
        _logger.Information("Received response for character {CharacterName}: {Response}", characterName, result.FullResponse);

        // Accumulate history for subsequent queries
        chatHistory.AddUserMessage(situation);
        chatHistory.AddUserMessage(query);
        chatHistory.AddAssistantMessage(result.FullResponse);

        return result;
    }

    private static Func<string, CharacterEmulationResult> CreateOutputParser()
    {
        return response =>
        {
            var observable = ResponseParser.ExtractText(response, "observable");
            return new CharacterEmulationResult(observable, response);
        };
    }

    private async Task<string> BuildInstruction(GenerationContext context) => await GetPromptAsync(context);

    protected override AgentName GetAgentName() => AgentName.CharacterPlugin;
}