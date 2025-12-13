using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;

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

    protected abstract string GetName();

    protected async Task<IKernelBuilder> GetKernelBuilder(Guid adventureId)
    {
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        var agentName = GetName();
        var preset = await dbContext
            .AdventureAgentLlmPresets
            .AsNoTracking()
            .Where(x => x.Id == adventureId && x.AgentName == agentName)
            .Select(x => x.LlmPreset)
            .SingleAsync();

        return KernelBuilderFactory.Create(preset);
    }
}