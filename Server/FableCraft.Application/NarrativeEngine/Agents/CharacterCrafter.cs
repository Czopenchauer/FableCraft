using System.Text.Json;

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

internal sealed class CharacterCrafter(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch)
{
    public async Task<CharacterContext> Invoke(
        GenerationContext context,
        CharacterRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = kernelBuilderFactory.Create(context.ComplexPreset);
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var trackerStructure = await dbContext
            .Adventures
            .Select(x => new { x.Id, x.TrackerStructure })
            .SingleAsync(x => x.Id == context.AdventureId, cancellationToken);

        var systemPrompt = await BuildInstruction(trackerStructure.TrackerStructure);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var promptContext = $"""
                             {PromptSections.Context(context.ContextGathered)}

                             {PromptSections.LastScenes(context.SceneContext, 3)}
                             """;
        chatHistory.AddUserMessage(promptContext);
        var creationRequestPrompt = $"""
                                     {PromptSections.CurrentScene(context.NewScene?.Scene)}

                                     Here is the character creation request for you to process:
                                     {PromptSections.CharacterCreationContext(request)}
                                     """;
        chatHistory.AddUserMessage(creationRequestPrompt);
        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), context.AdventureId));
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        var outputParser = CreateOutputParser();

        (CharacterStats characterStats, string description, CharacterTracker tracker, CharacterDevelopmentTracker developmentTracker) result =
            await agentKernel.SendRequestAsync(
                chatHistory,
                outputParser,
                kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
                nameof(CharacterCrafter),
                kernelWithKg,
                cancellationToken);

        return new CharacterContext
        {
            CharacterId = Guid.NewGuid(),
            CharacterState = result.characterStats,
            Description = result.description,
            CharacterTracker = result.tracker,
            DevelopmentTracker = result.developmentTracker,
            Name = result.characterStats.CharacterIdentity.FullName!,
            SequenceNumber = 1
        };
    }

    private static Func<string, (CharacterStats characterStats, string description, CharacterTracker tracker, CharacterDevelopmentTracker developmentTracker)>
        CreateOutputParser()
    {
        return response =>
        {
            var characterStats = ResponseParser.ExtractJson<CharacterStats>(response, "character");
            var tracker = ResponseParser.ExtractJson<CharacterTracker>(response, "character_statistics");
            var developmentTracker = ResponseParser.ExtractJson<CharacterDevelopmentTracker>(response, "character_development");
            var description = ResponseParser.ExtractText(response, "character_description");

            if (string.IsNullOrEmpty(description))
            {
                throw new InvalidCastException("Failed to parse character description from response due to empty description.");
            }

            return (characterStats, description, tracker, developmentTracker);
        };
    }

    private async static Task<string> BuildInstruction(TrackerStructure structure)
    {
        JsonSerializerOptions options = PromptSections.GetJsonOptions();

        var prompt = await PromptBuilder.BuildPromptAsync("CharacterCrafterPrompt.md");
        return prompt
            .Replace("{{character_tracker_format}}", JsonSerializer.Serialize(GetSystemPrompt(structure), options))
            .Replace("{{character_tracker}}", JsonSerializer.Serialize(GetOutputJson(structure), options))
            .Replace("{{character_development_format}}", JsonSerializer.Serialize(TrackerExtensions.ConvertToSystemJson(structure.CharacterDevelopment!), options))
            .Replace("{{character_development}}", JsonSerializer.Serialize(TrackerExtensions.ConvertToOutputJson(structure.CharacterDevelopment!), options));
    }

    private static Dictionary<string, object> GetOutputJson(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToOutputJson(structure.Characters);
    }

    private static Dictionary<string, object> GetSystemPrompt(TrackerStructure structure)
    {
        return TrackerExtensions.ConvertToSystemJson(structure.Characters);
    }
}