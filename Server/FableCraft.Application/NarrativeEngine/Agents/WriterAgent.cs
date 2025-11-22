using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class WriterAgent(IAgentKernel agentKernel)
{
    public async Task<GeneratedScene> Invoke(NarrativeContext context, NarrativeDirectorOutput narrativeDirectorOutput, CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        var promptContext = $"""
                             <scene_direction>
                             {narrativeDirectorOutput.SceneDirection}
                             </scene_direction>
                             """;
        chatHistory.AddUserMessage(context.CommonContext);
        chatHistory.AddUserMessage(promptContext);
        var outputFunc = new Func<string, GeneratedScene>(response =>
        {
            var match = Regex.Match(response, "<new_scene>(.*?)</new_scene>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<GeneratedScene>(match.Groups[1].Value) ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse CharacterStats from response due to stats not being in correct tags.");
        });
        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputFunc,
            cancellationToken,
            context.KernelKg);
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "WriterPrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}