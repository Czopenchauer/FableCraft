using System.Reflection;
using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace FableCraft.Infrastructure.Queue;

internal class InMemoryMessageDispatcher(Channel<IMessage> channel) : IMessageDispatcher
{
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage
    {
        await channel.Writer.WriteAsync(message, cancellationToken);
    }
}

internal class InMemoryMessageReader(IServiceProvider serviceProvider, Channel<IMessage> channel, ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Information("Starting In-Memory Message Reader...");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (await channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (channel.Reader.TryRead(out IMessage? message))
                    {
                        ArgumentNullException.ThrowIfNull(message);
                        _ = Task.Run(async () =>
                            {
                                try
                                {
                                    ProcessExecutionContext.AdventureId.Value = message.AdventureId;
                                    Type messageType = message.GetType();
                                    Type handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
                                    await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
                                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                                    MethodInfo? handleMethod =
                                        handlerType.GetMethod(nameof(IMessageHandler<>.HandleAsync));
                                    await (Task)handleMethod!.Invoke(handler, [message, CancellationToken.None])!;
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(ex, "Error processing message of type {MessageType}", message.GetType().Name);
                                }
                            },
                            stoppingToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unhandled exception in In-Memory Message Reader");
        }
    }
}