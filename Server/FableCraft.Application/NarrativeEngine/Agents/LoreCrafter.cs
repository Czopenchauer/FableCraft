using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.SemanticKernel;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class LoreCrafter(
    IAgentKernel agentKernel,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<GeneratedLore> Invoke(
        GenerationContext context,
        LoreRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        var systemPrompt = await PromptBuilder.BuildPromptAsync("LoreCrafterPrompt.md");

        var chatHistory = ChatHistoryBuilder.Create()
            .WithSystemMessage(systemPrompt)
            .WithCreatedCharacters(context.NewCharacters)
            .WithLoreCreationContext(request)
            .WithContext(context.ContextGathered)
            .WithPreviousScene(context.SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.SceneContent)
            .WithCurrentScene(context.NewScene?.Scene)
            .Build();

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        // LoreCrafter doesn't use XML tags, it returns raw JSON
        var outputParser = ResponseParser.CreateRawJsonParser<GeneratedLore>();

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(LoreCrafter),
            kernelWithKg,
            cancellationToken);
    }
}
