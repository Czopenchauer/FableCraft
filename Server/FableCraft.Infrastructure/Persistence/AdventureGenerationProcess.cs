using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

namespace FableCraft.Infrastructure.Persistence;

public class AdventureGenerationProcess : IEntity
{
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    public ProcessingStatus RagProcessingStatus { get; set; }

    public ProcessingStatus SceneGenerationStatus { get; set; }
}