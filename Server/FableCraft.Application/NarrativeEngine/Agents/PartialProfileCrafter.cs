using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Creates lightweight character profiles for background characters.
/// Output is stored as LorebookEntry in World KG, not as Character entity.
/// </summary>
internal sealed class PartialProfileCrafter(
    IAgentKernel agentKernel,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    KernelBuilderFactory kernelBuilderFactory,
    IPluginFactory pluginFactory) : BaseAgent(dbContextFactory, kernelBuilderFactory)
{
    protected override AgentName GetAgentName() => AgentName.PartialProfileCrafter;

    public async Task<GeneratedPartialProfile> Invoke(
        GenerationContext context,
        CharacterRequest request,
        CancellationToken cancellationToken)
    {
        IKernelBuilder kernelBuilder = await GetKernelBuilder(context);
        var systemPrompt = await GetPromptAsync(context);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = $"""
                             {PromptSections.CurrentStoryTracker(context)}

                             {PromptSections.WorldSettings(context.PromptPath)}

                             {PromptSections.PreviousScene(context.SceneContext.OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.SceneContent)}
                             """;
        chatHistory.AddUserMessage(contextPrompt);

        var requestPrompt = $"""
                             {PromptSections.WorldSettings(context.PromptPath)}

                             {PromptSections.CurrentScene(context)}

                             Create a partial profile for this background character based on the following request:
                             {PromptSections.CharacterCreationContext(request)}
                             """;
        chatHistory.AddUserMessage(requestPrompt);

        Microsoft.SemanticKernel.IKernelBuilder kernel = kernelBuilder.Create();
        var callerContext = new CallerContext(GetType(), context.AdventureId);
        await pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, context, callerContext);
        Kernel kernelWithKg = kernel.Build();

        var outputParser = ResponseParser.CreateJsonParser<GeneratedPartialProfile>("partial_profile");

        return await agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            kernelBuilder.GetDefaultFunctionPromptExecutionSettings(),
            nameof(PartialProfileCrafter),
            kernelWithKg,
            cancellationToken);
    }
}
