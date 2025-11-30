using FableCraft.Infrastructure.Queue;

namespace FableCraft.Tests.Agents;

internal sealed class MockMessageDispatcher : IMessageDispatcher
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : IMessage
    {
        return Task.CompletedTask;
    }
}