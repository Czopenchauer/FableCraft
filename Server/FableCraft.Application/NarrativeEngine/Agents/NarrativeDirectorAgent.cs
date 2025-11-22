using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class NarrativeDirectorAgent(IAgentKernel agentKernel)
{
    public async Task<NarrativeDirectorOutput> Invoke(NarrativeContext context, CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        var systemPrompt = await BuildInstruction();
        chatHistory.AddSystemMessage(systemPrompt);
        if (context.GetCurrentSceneMetadata() != null)
        {
            var narrativeDirection = JsonSerializer.Serialize(context.GetCurrentSceneMetadata()?.NarrativeMetadata);
            var promptContext = $"""
                                 <last_scene_narrative_direction>
                                 {narrativeDirection}
                                 </last_scene_narrative_direction>
                                 """;

            chatHistory.AddUserMessage(promptContext);
        }

        chatHistory.AddUserMessage(context.CommonContext);
        chatHistory.AddUserMessage(context.PlayerAction);
        var outputFunc = new Func<string, NarrativeDirectorOutput>(response =>
        {
            var match = Regex.Match(response, "<narrative_scene_directive>(.*?)</narrative_scene_directive>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<NarrativeDirectorOutput>(match.Groups[1].Value) ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse NarrativeDirectorOutput from response due to output not being in correct tags.");
        });
        return await agentKernel.SendRequestAsync(chatHistory, outputFunc, cancellationToken, context.KernelKg);
    }

    private async static Task<string> BuildInstruction()
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "NarrativeEngine",
            "Agents",
            "Prompts",
            "NarrativePrompt.md"
        );

        return await File.ReadAllTextAsync(promptPath);
    }
}