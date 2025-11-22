using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class LoreCrafter
{
    private readonly IAgentKernel _agentKernel;

    public LoreCrafter(IAgentKernel agentKernel)
    {
        _agentKernel = agentKernel;
    }

    public async Task<GeneratedLore> Invoke(
        Kernel kernel,
        LoreRequest request,
        CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var contextPrompt = $"""
                             <lore_creation_context>
                             {JsonSerializer.Serialize(request, options)}
                             </lore_creation_context>
                             """;
        chatHistory.AddUserMessage(contextPrompt);
        var outputFunc = new Func<string, GeneratedLore>(response => JsonSerializer.Deserialize<GeneratedLore>(response.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options) ?? throw new InvalidOperationException());

        return await _agentKernel.SendRequestAsync(chatHistory, outputFunc, cancellationToken, kernel: kernel);
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "LoreCrafterPrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}