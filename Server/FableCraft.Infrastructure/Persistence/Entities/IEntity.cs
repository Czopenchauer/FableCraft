namespace FableCraft.Infrastructure.Persistence.Entities;

public interface IEntity
{
    Guid Id { get; set; }
}

public interface IKnowledgeGraphEntity : IEntity
{
    public string GetContent();

    public string GetContentDescription();
}