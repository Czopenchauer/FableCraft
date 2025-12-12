using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class ItemCrafter(
    IAgentKernel agentKernel,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<GeneratedItem> Invoke(
        GenerationContext context,
        ItemRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        var systemPrompt = await PromptBuilder.BuildPromptAsync("ItemCrafterPrompt.md");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.CreatedCharacters(context.NewCharacters)}

                             {PromptSections.Context(context.ContextGathered)}

                             {PromptSections.PreviousScene(context.SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.SceneContent)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.CurrentScene(context.NewScene?.Scene)}

                             Create new item based on the following request:
                             {PromptSections.ItemRequest(request)}
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<GeneratedItem>("item");

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(ItemCrafter),
            kernelWithKg,
            cancellationToken);
    }
}
