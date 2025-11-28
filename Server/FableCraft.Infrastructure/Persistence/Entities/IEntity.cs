namespace FableCraft.Infrastructure.Persistence.Entities;

public interface IEntity
{
    Guid Id { get; set; }
}

public class Content
{
    public string Text { get; }

    public string Description { get; }

    public string ContentType { get; }

    public DateTime ReferenceTime { get; set; }

    public Content(string text, string description, ContentType contentType, DateTime? referenceTime = null)
    {
        Text = text;
        Description = description;
        ContentType = contentType.ToString().ToLowerInvariant();
        ReferenceTime = referenceTime ?? DateTime.UtcNow;
    }
}

public enum ContentType
{
    Json,
    Txt
}

public interface IKnowledgeGraphEntity : IEntity
{
    public Content GetContent();
}