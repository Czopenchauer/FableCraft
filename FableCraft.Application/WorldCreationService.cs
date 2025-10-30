using FableCraft.Application.Validators;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Queue;

namespace FableCraft.Application;

public class WorldCreationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMessageDispatcher _messageDispatcher;

    public WorldCreationService(ApplicationDbContext dbContext, IMessageDispatcher messageDispatcher)
    {
        _dbContext = dbContext;
        _messageDispatcher = messageDispatcher;
    }

    public async Task<Guid> CreateWorldAsync(WorldDto worldDto, CancellationToken cancellationToken)
    {
        var worldId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var world = new World
        {
            WorldId = worldId,
            Name = worldDto.Name,
            Backstory = worldDto.Backstory,
            UniverseBackstory = worldDto.UniverseBackstory,
            CreatedAt = now,
            LastPlayedAt = now,
            ProcessingStatus = ProcessingStatus.Pending,
            CharacterId = characterId,
            Character = new Character
            {
                CharacterId = characterId,
                Name = worldDto.Character.Name,
                Description = worldDto.Character.Description,
                Background = worldDto.Character.Background,
                ProcessingStatus = ProcessingStatus.Pending,
                StatsJson = "{}",
                CreatedAt = now,
                LastUpdatedAt = now
            },
            Lorebook = worldDto.Lorebook.Select(entry => new LorebookEntry
            {
                EntryId = Guid.NewGuid(),
                WorldId = worldId,
                Title = entry.Title,
                Content = entry.Content,
                Category = entry.Category,
                ProcessingStatus = ProcessingStatus.Pending,
                CreatedAt = now,
                LastUpdatedAt = now
            }).ToList(),
            Scenes = new List<Scene>()
        };

        await _dbContext.Worlds.AddAsync(world, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return worldId;
    }
}