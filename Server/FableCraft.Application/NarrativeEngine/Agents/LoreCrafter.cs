using System.Text.Json;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class LoreCrafter(IAgentKernel agentKernel, KernelBuilderFactory kernelBuilderFactory, IRagSearch ragSearch)
{
    public async Task<GeneratedLore> Invoke(
        GenerationContext context,
        LoreRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        if (context.NewCharacters?.Length > 0)
        {
            var createdCharactersJson = JsonSerializer.Serialize(context.NewCharacters, options);
            chatHistory.AddUserMessage($"""
                                         <created_characters>
                                         {createdCharactersJson}
                                         </created_characters>
                                        """);
        }

        chatHistory.AddUserMessage($"""
                                    <lore_creation_context>
                                    {JsonSerializer.Serialize(request, options)}
                                    </lore_creation_context>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <context>
                                    {JsonSerializer.Serialize(context.ContextGathered, options)}
                                    </context>
                                    """);

        chatHistory.AddUserMessage($"""
                                    <previous_scene>
                                    {context.SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.SceneContent ?? string.Empty}
                                    <previous_scene>
                                    """);

        if (context.NewScene != null)
        {
            chatHistory.AddUserMessage($"""
                                        <current_scene>
                                        {context.NewScene!.Scene}
                                        <current_scene>
                                        """);
        }

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        var outputFunc = new Func<string, GeneratedLore>(response =>
            JsonSerializer.Deserialize<GeneratedLore>(response.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options) ?? throw new InvalidOperationException());

        return await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(LoreCrafter),
            kernelWithKg,
            cancellationToken);
    }

    private static Task<string> BuildInstruction()
    {
        return PromptBuilder.BuildPromptAsync("LoreCrafterPrompt.md");
    }
}