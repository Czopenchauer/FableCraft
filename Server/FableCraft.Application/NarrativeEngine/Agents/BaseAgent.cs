using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application.NarrativeEngine.Agents;

internal abstract class BaseAgent
{
    protected readonly IDbContextFactory<ApplicationDbContext> DbContextFactory;
    protected readonly KernelBuilderFactory KernelBuilderFactory;

    protected BaseAgent(IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory)
    {
        DbContextFactory = dbContextFactory;
        KernelBuilderFactory = kernelBuilderFactory;
    }

    protected abstract AgentName GetAgentName();

    /// <summary>
    /// Gets the prompt for the specified agent. Prompt file must be named {AgentName}.md
    /// </summary>
    protected async Task<string> GetPromptAsync(GenerationContext generationContext)
    {
        var agentName = GetAgentName();
        var agentPromptPath = Path.Combine(
            generationContext.PromptPath,
            $"{agentName}.md"
        );
        var storyBible = await File.ReadAllTextAsync(Path.Combine(generationContext.PromptPath, "StoryBible.md"));

        var promptTemplate = await File.ReadAllTextAsync(agentPromptPath);
        var promp = await ReplaceJailbreakPlaceholder(promptTemplate, generationContext.PromptPath);
        return promp.Replace("{{story_bible}}", storyBible);
    }

    private async static Task<string> ReplaceJailbreakPlaceholder(string promptTemplate, string promptPath)
    {
        if (!promptTemplate.Contains(PlaceholderNames.Jailbreak))
        {
            return promptTemplate;
        }

        var filePath = Path.Combine(
            promptPath,
            "Jailbrake.md"
        );

        if (File.Exists(filePath))
        {
            var fileContent = await File.ReadAllTextAsync(filePath);
            return promptTemplate.Replace(PlaceholderNames.Jailbreak, fileContent);
        }

        return promptTemplate;
    }

    protected async Task<IKernelBuilder> GetKernelBuilder(GenerationContext generationContext)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        var agentName = GetAgentName();
        var adventureAgentLlmPreset = generationContext.AgentLlmPreset.Single(x => x.AgentName == agentName);

        return KernelBuilderFactory.Create(adventureAgentLlmPreset.LlmPreset);
    }
}