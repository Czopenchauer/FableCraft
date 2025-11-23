using System.Text.Json;
using System.Text.RegularExpressions;

using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal sealed class WriterAgent(IAgentKernel agentKernel, ILogger logger)
{
    public async Task<GeneratedScene> Invoke(
        NarrativeContext context,
        CharacterContext[] characterContexts,
        GeneratedLore[] newLore,
        NarrativeDirectorOutput narrativeDirectorOutput,
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
        var promptContext = $"""
                             <scene_direction>
                             {JsonSerializer.Serialize(narrativeDirectorOutput.SceneDirection, options)}
                             </scene_direction>

                             <new_lore>
                             {JsonSerializer.Serialize(newLore, options)}
                             </new_lore>

                             Newly created characters. Should be emulated as well as existing ones:
                             <new_characters>
                             {JsonSerializer.Serialize(characterContexts, options)}
                             </new_characters>
                             """;
        chatHistory.AddUserMessage(context.CommonContext);
        chatHistory.AddUserMessage(promptContext);

        var characterPlugin = new CharacterPlugin(agentKernel, logger);
        await characterPlugin.Setup(context);
        var kernel = context.KernelKg.Clone();
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(characterPlugin));

        var outputFunc = new Func<string, GeneratedScene>(response =>
        {
            var match = Regex.Match(response, "<new_scene>(.*?)</new_scene>", RegexOptions.Singleline);
            if (match.Success)
            {
                return JsonSerializer.Deserialize<GeneratedScene>(match.Groups[1].Value.RemoveThinkingBlock().ExtractJsonFromMarkdown(), options)
                       ?? throw new InvalidOperationException();
            }

            throw new InvalidCastException("Failed to parse New Scene from response due to scene not being in correct tags.");
        });
        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputFunc,
            cancellationToken,
            kernel);
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