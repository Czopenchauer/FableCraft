using System.Text.Json;

using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal class ContextBase
{
    public required string Query { get; set; }

    public required string Response { get; set; }
}

internal sealed class ContextGatherer(
    IAgentKernel agentKernel,
    IKernelBuilder kernelBuilder,
    IRagSearch ragSearch,
    ILogger logger)
{
    public async Task<List<ContextBase>> Invoke(
        Guid adventureId,
        string context,
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

        chatHistory.AddUserMessage(context);
        var outputFunc = new Func<string, string[]>(response =>
            JsonSerializer.Deserialize<string[]>(response.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options) ?? throw new InvalidOperationException());
        var promptExecutionSettings = kernelBuilder.GetDefaultPromptExecutionSettings();
        promptExecutionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
        var queries = await agentKernel.SendRequestAsync(chatHistory, outputFunc, cancellationToken, promptExecutionSettings: promptExecutionSettings);
        var tasks = queries
            .Select(x => ragSearch.SearchAsync(adventureId.ToString(), x, cancellationToken)
                .ContinueWith(task => new ContextBase
                    {
                        Query = x,
                        Response = task.Result.Content!
                    },
                    cancellationToken)).ToList();

        List<ContextBase> results = [];
        foreach (var task in tasks)
        {
            try
            {
                results.Add(await task);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error gathering context for adventure {AdventureId}", adventureId);
                // Best effort - continue with other tasks. Other agents can query the RAG system again if needed.
            }
        }
        return results;
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "ContextBuilderAgent.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}