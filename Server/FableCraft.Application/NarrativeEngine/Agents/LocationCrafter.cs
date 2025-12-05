using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class LocationCrafter(
    IAgentKernel agentKernel,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<LocationGenerationResult> Invoke(
        GenerationContext context,
        LocationRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.LlmPreset);
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

        var contextPrompt = $"""
                             <location_request>
                             {JsonSerializer.Serialize(request, options)}
                             </location_request>

                             <context>
                             {JsonSerializer.Serialize(context.ContextGathered, options)}
                             </context>
                             """;
        chatHistory.AddUserMessage(contextPrompt);
        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();
        var outputFunc = new Func<string, LocationGenerationResult>(response =>
        {
            Match match = Regex.Match(response, "<location>(.*?)</location>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<LocationGenerationResult>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse LocationGenerationResult from response due to output not being in correct tags.");
        });

        return await agentKernel.SendRequestAsync(chatHistory,
            outputFunc,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(LocationCrafter),
            kernelWithKg,
            cancellationToken);
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "LocationCrafterPrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}