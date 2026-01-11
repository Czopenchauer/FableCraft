using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FableCraft.Application.NarrativeEngine;

internal sealed class UnlockChunks(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Scenes
            .Where(x => x.CommitStatus == CommitStatus.Lock)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.CommitStatus, CommitStatus.Uncommited), cancellationToken);

        var processes = await dbContext.GenerationProcesses
            .Where(x => x.Step == GenerationProcessStep.GeneratingScene)
            .ToArrayAsync(cancellationToken);

        var adventures = await dbContext.Adventures
            .Where(x => x.RagProcessingStatus == ProcessingStatus.InProgress || x.SceneGenerationStatus == ProcessingStatus.InProgress)
            .ToArrayAsync(cancellationToken);

        foreach (var adventure in adventures)
        {
            if (adventure.RagProcessingStatus == ProcessingStatus.InProgress)
            {
                adventure.RagProcessingStatus = ProcessingStatus.Pending;
            }

            if (adventure.SceneGenerationStatus == ProcessingStatus.InProgress)
            {
                adventure.SceneGenerationStatus = ProcessingStatus.Pending;
            }
        }

        foreach (var generationProcess in processes)
        {
            if (generationProcess.Step == GenerationProcessStep.GeneratingScene)
            {
                generationProcess.Step = GenerationProcessStep.NotStarted;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}