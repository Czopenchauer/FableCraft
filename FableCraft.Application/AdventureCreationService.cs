using FableCraft.Application.Exceptions;
using FableCraft.Application.Model;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Application;

public enum Status
{
    Pending,
    Completed,
    Failed
}

public static class StatusExtensions
{
    public static Status ToStatus(this ProcessingStatus processingStatus) =>
        processingStatus switch
        {
            ProcessingStatus.Pending => Status.Pending,
            ProcessingStatus.Completed => Status.Completed,
            ProcessingStatus.Failed => Status.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(processingStatus), processingStatus, null)
        };
}

public class AdventureCreationStatus
{
    public AdventureCreationStatus(Adventure adventure)
    {
        AdventureId = adventure.Id;

        var statusDict = new Dictionary<string, Status>
        {
            { "World Description", adventure.ProcessingStatus.ToStatus() },
            { "Character", adventure.Character.ProcessingStatus.ToStatus() }
        };

        foreach (var entry in adventure.Lorebook)
        {
            statusDict.Add(entry.Category, entry.ProcessingStatus.ToStatus());
        }

        ComponentStatuses = statusDict;
    }

    public Guid AdventureId { get; }

    public Dictionary<string, Status> ComponentStatuses { get; }
}

public class AdventureCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly TimeProvider _timeProvider;

    public AdventureCreationService(ApplicationDbContext dbContext, IMessageDispatcher messageDispatcher, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
        _timeProvider = timeProvider;
    }

    public async Task<AdventureCreationStatus> CreateAdventureAsync(AdventureDto adventureDto, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var world = new Adventure
        {
            Name = adventureDto.Name,
            WorldDescription = adventureDto.Description,
            CreatedAt = now,
            LastPlayedAt = now,
            ProcessingStatus = ProcessingStatus.Pending,
            Character = new Character
            {
                Name = adventureDto.Character.Name,
                Description = adventureDto.Character.Description,
                Background = adventureDto.Character.Background,
                ProcessingStatus = ProcessingStatus.Pending,
                StatsJson = "{}",
            },
            Lorebook = adventureDto.Lorebook.Select(entry => new LorebookEntry
            {
                Title = entry.Title,
                Content = entry.Content,
                Category = entry.Category,
                ProcessingStatus = ProcessingStatus.Pending,
            }).ToList(),
            Scenes = new List<Scene>()
        };

        _dbContext.Adventures.Add(world);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _messageDispatcher.PublishAsync(new AddAdventureToKnowledgeGraphCommand
        {
            AdventureId = world.Id
        }, cancellationToken);

        return new AdventureCreationStatus(world);
    }
    
    public async Task<AdventureCreationStatus> GetAdventureCreationStatusAsync(Guid worldId, CancellationToken cancellationToken)
    {
        var world = await _dbContext.Adventures
            .Include(w => w.Character)
            .Include(w => w.Lorebook)
            .FirstOrDefaultAsync(w => w.Id == worldId, cancellationToken);

        if (world == null)
        {
            throw new AdventureNotFoundException(worldId);
        }

        return new AdventureCreationStatus(world);
    }
}