using Microsoft.Extensions.Diagnostics.Enrichment;

namespace FableCraft.Infrastructure.Queue;

public interface IMessage
{
    public Guid AdventureId { get; set; }
}

internal interface IMessageWithEnrichment : IMessage
{
    internal ILogEnricher TraceId { get; set; }
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