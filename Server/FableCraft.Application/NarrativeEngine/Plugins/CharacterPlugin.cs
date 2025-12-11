using System.ComponentModel;
using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Plugins;

internal sealed class CharacterPlugin(
    IAgentKernel agentKernel,
    ILogger logger,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    private Dictionary<string, ChatHistory> _chatHistory = new();
    private GenerationContext _generationContext = null!;
    private IKernelBuilder _kernelBuilder = null!;

    public async Task Setup(GenerationContext generationContext)
    {
        _generationContext = generationContext;
        _kernelBuilder = kernelBuilderFactory.Create(generationContext.LlmPreset);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var promptTemplate = await BuildInstruction();
        var characters = new List<CharacterContext>(generationContext.Characters);
        characters.AddRange(generationContext.NewCharacters ?? Array.Empty<CharacterContext>());
        _chatHistory = characters.ToDictionary(character => character.Name,
            context =>
            {
                var systemPrompt = promptTemplate.Replace("{CHARACTER_NAME}", context.Name);
                var chatHistory = new ChatHistory(systemPrompt);
                chatHistory.AddUserMessage(
                    $"""
                     <character_description>
                     {context.Description}
                     </character_description>

                     <character_state>
                     {JsonSerializer.Serialize(context.CharacterState, options)}
                     </character_state>

                     <character_tracker>
                     {JsonSerializer.Serialize(context.CharacterTracker, options)}
                     </character_tracker>
                     
                     <character_development_tracker>
                     {JsonSerializer.Serialize(context.DevelopmentTracker, options)}
                     </character_development_tracker>

                     <context>
                     {JsonSerializer.Serialize(_generationContext.ContextGathered, options)}
                     </context>

                     CRITICAL! These scenes are written in the perspective of story protagonist - {generationContext.MainCharacter.Name}!
                     <previous_scenes_with_character>
                     {string.Join("\n\n---\n\n", _generationContext
                         .SceneContext
                         .Where(x => x.Metadata.Tracker?.Characters?.Select(y => y.Name).Contains(context.Name) == true)
                         .OrderByDescending(x => x.SequenceNumber)
                         .TakeLast(5)
                         .Select(s => $"""
                                       SCENE NUMBER: {s.SequenceNumber}
                                       TIME: {s.Metadata.Tracker?.Story.Time} - LOCATION: {s.Metadata.Tracker?.Story.Location}
                                       {s.SceneContent}
                                       {s.PlayerChoice}
                                       """))}
                     </previous_scenes_with_character>

                     <current_time>
                     {_generationContext.SceneContext.MaxBy(x => x.SequenceNumber)?.Metadata.Tracker?.Story.Time}
                     </current_time>
                     <current_location>
                     {_generationContext.SceneContext.MaxBy(x => x.SequenceNumber)?.Metadata.Tracker?.Story.Location}
                     </current_location>
                     """);

                return chatHistory;
            });
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
        logger.Information("Emulating action for character {CharacterName} in situation: {Situation}", characterName, situation);
        if (!_chatHistory.TryGetValue(characterName, out ChatHistory? chatHistory))
        {
            logger.Information("Character {CharacterName} not found in chat history. Current chatHistory: {history}", characterName, _chatHistory.Keys);
            return "Character not found.";
        }

        Microsoft.SemanticKernel.IKernelBuilder kernel = _kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), _generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        chatHistory.AddUserMessage(situation);
        var outputFunc = new Func<string, string>(response => response);
        var response = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            _kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            $"{nameof(CharacterPlugin)}:{characterName}",
            kernelWithKg,
            CancellationToken.None);
        logger.Information("Received response for character {CharacterName}: {Response}", characterName, response);
        return response;
    }

    private async static Task<string> BuildInstruction()
    {
        return await Agents.PromptBuilder.BuildPromptAsync("CharacterPrompt.md");
    }
}