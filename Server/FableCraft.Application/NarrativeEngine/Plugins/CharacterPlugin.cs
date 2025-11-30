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
    IKernelBuilder kernelBuilder,
    IRagSearch ragSearch)
{
    private Dictionary<string, ChatHistory> _chatHistory = new();
    private GenerationContext _generationContext = null!;

    public async Task Setup(GenerationContext generationContext)
    {
        _generationContext = generationContext;
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        var promptTemplate = await BuildInstruction();
        _chatHistory = generationContext.Characters.ToDictionary(character => character.Name,
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

        var kernel = kernelBuilder.WithBase();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), _generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        chatHistory.AddUserMessage(situation);
        var outputFunc = new Func<string, string>(response => response);
        return await agentKernel.SendRequestAsync(chatHistory, outputFunc, kernelBuilder.GetDefaultFunctionPromptExecutionSettings(), CancellationToken.None, kernelWithKg);
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "CharacterPrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}