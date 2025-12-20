using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using static FableCraft.Infrastructure.Clients.RagClientExtensions;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Character Reflection Agent - runs post-scene for each meaningful character present.
/// Replaces CharacterStateAgent and adds scene rewriting from character's POV.
///
/// Output:
/// - scene_rewrite: Full character-POV prose -> stored in KG
/// - memory: Summary, salience, entities, emotional_tone -> stored in DB
/// - relationship_updates: Per-character relationship state -> stored in DB
/// - emotional_state, goals_and_motivations, current_plan -> stored in CharacterStats
/// </summary>
internal sealed class CharacterReflectionAgent(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IRagSearch ragSearch) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.CharacterReflectionAgent;

    public async Task<CharacterReflectionOutput> Invoke(
        GenerationContext generationContext,
        CharacterContext context,
        StoryTracker storyTrackerResult,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(generationContext);

        var systemPrompt = await BuildInstruction(generationContext, context.Name);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.WorldSettings(generationContext.WorldSettings)}

                             {PromptSections.StoryTracker(storyTrackerResult, true)}

                             {PromptSections.NewItems(generationContext.NewItems)}

                             {PromptSections.RecentScenesForCharacter(context)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.CharacterStateContext(context)}

                             {PromptSections.CurrentScene(generationContext)}

                             Reflect on this scene from {context.Name}'s perspective. Generate your output with only the sections that changed.
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        var outputParser = ResponseParser.CreateJsonParser<CharacterReflectionOutput>("character_reflection", true);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var datasets = new List<string>
        {
            GetCharacterDatasetName(generationContext.AdventureId, context.CharacterId)
        };
        var kgPlugin = new KnowledgeGraphPlugin(ragSearch, new CallerContext(GetType(), generationContext.AdventureId), datasets);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(kgPlugin));
        Kernel kernelWithKg = kernel.Build();

        PromptExecutionSettings promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(CharacterReflectionAgent),
            kernelWithKg,
            cancellationToken);
    }

    private async Task<string> BuildInstruction(GenerationContext context, string characterName)
    {
        var prompt = await GetPromptAsync(context);
        return PromptBuilder.ReplacePlaceholders(prompt,
            (PlaceholderNames.CharacterName, characterName));
    }
}
