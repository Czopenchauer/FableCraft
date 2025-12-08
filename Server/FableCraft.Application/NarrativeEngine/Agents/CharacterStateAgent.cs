using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class CharacterStateAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<CharacterStats> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(generationContext.LlmPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction(context.Name);
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        chatHistory.AddUserMessage($"""
                                    <previous_character_state>
                                    {JsonSerializer.Serialize(context.CharacterState, options)}
                                    </previous_character_state>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <recent_scenes>
                                    {string.Join("\n\n---\n\n", (generationContext.SceneContext ?? Array.Empty<SceneContext>())
                                        .OrderByDescending(x => x.SequenceNumber)
                                        .TakeLast(3)
                                        .Select(s => $"""
                                                      SCENE NUMBER: {s.SequenceNumber}
                                                      {s.SceneContent}
                                                      {s.PlayerChoice}
                                                      """))}
                                    </recent_scenes>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <current_scene>
                                    {generationContext.NewScene?.Scene ?? generationContext.PlayerAction}
                                    </current_scene>
                                    """);

        var instruction = "Update the character state based on the new scene content and previous character state.";
        chatHistory.AddUserMessage(instruction);

        var outputFunc = new Func<string, CharacterStats>(response =>
        {
            Match match = Regex.Match(response, "<character_state>(.*?)</character_state>", RegexOptions.Singleline);
            CharacterStats state;
            if (match.Success)
            {
                state = JsonSerializer.Deserialize<CharacterStats>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                        ?? throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException("Failed to parse CharacterState from response due to output not being in correct tags.");
            }

            return state;
        });

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        CharacterStats result = await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            promptExecutionSettings,
            nameof(CharacterStateAgent),
            kernelWithKg,
            cancellationToken);
        return result;
    }

    private async static Task<string> BuildInstruction(string characterName)
    {
        var prompt = await PromptBuilder.BuildPromptAsync("CharacterStatePrompt.md");
        return prompt.Replace("{CHARACTER_NAME}", characterName);
    }
}
