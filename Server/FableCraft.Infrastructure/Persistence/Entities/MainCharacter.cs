namespace FableCraft.Infrastructure.Persistence.Entities;

public sealed class MainCharacter : IKnowledgeGraphEntity
{
    public Guid Id { get; set; }

    public Content GetContent()
    {
        return new Content(Description,
            Name,
            ContentType.Text);
    }

    public Guid AdventureId { get; set; }

    public required string Name { get; init; }

    public required string Description { get; init; }
}