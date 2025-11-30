namespace FableCraft.Infrastructure.Queue;

public interface IMessage
{
    public Guid AdventureId { get; set; }
}

public interface IMessageHandler<in TMessage> where TMessage : IMessage
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken);
}

public interface IMessageDispatcher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage;
}