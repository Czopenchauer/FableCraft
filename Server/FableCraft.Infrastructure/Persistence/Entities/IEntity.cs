namespace FableCraft.Infrastructure.Persistence.Entities;

public interface IEntity
{
    Guid Id { get; set; }
}

public enum ContentType
{
    json,
    txt
}