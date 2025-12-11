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
        Tracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(generationContext.LlmPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var systemPrompt = await BuildInstruction(context.Name);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(PromptSections.StoryTracker(storyTrackerResult, true));
        chatHistory.AddUserMessage(PromptSections.CharacterStateContext(context, true));
        chatHistory.AddUserMessage(PromptSections.RecentScenesForCharacter(
            generationContext.SceneContext ?? [],
            generationContext.MainCharacter.Name,
            context.Name,
            3));
        chatHistory.AddUserMessage(PromptSections.CurrentScene(generationContext.NewScene!.Scene));

        var outputParser = ResponseParser.CreateJsonParser<CharacterStats>("character_state", true);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), generationContext.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(CharacterStateAgent),
            kernelWithKg,
            cancellationToken);
    }

    private static async Task<string> BuildInstruction(string characterName)
    {
        var prompt = await PromptBuilder.BuildPromptAsync("CharacterStatePrompt.md");
        return prompt.Replace("{CHARACTER_NAME}", characterName);
    }
}
