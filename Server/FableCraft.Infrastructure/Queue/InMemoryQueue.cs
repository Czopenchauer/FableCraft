using System.Diagnostics;
using System.Reflection;
using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Context;
using Serilog.Core;

namespace FableCraft.Infrastructure.Queue;

internal record MessageWithContext(IMessage Message, ILogEventEnricher? LogContext, Activity? Activity);

internal class InMemoryMessageDispatcher(Channel<MessageWithContext> channel) : IMessageDispatcher
{
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage
    {
        ILogEventEnricher context = LogContext.Clone();
        Activity? currentActivity = Activity.Current;
        IMessage actualMessage = (message as IMessageWithEnrichment) ?? (IMessage)message;
        var messageWithContext = new MessageWithContext(actualMessage, context, currentActivity);
        await channel.Writer.WriteAsync(messageWithContext, cancellationToken);
    }
}

internal class InMemoryMessageReader(IServiceProvider serviceProvider, Channel<MessageWithContext> channel, ILogger logger) : BackgroundService
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
                    while (channel.Reader.TryRead(out MessageWithContext? messageWithContext))
                    {
                        ArgumentNullException.ThrowIfNull(messageWithContext);
                        _ = Task.Run(async () =>
                            {
                                IMessage message = messageWithContext.Message;
                                try
                                {
                                    using var linkedActivity = new Activity("ProcessMessage")
                                        .SetParentId(messageWithContext.Activity!.Id!)
                                        .Start();

                                    using (messageWithContext.LogContext is not null
                                               ? LogContext.Push(messageWithContext.LogContext)
                                               : null)
                                    {
                                        ProcessExecutionContext.AdventureId.Value = message.AdventureId;
                                        Type messageType = message.GetType();
                                        Type handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
                                        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
                                        var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                                        MethodInfo? handleMethod =
                                            handlerType.GetMethod(nameof(IMessageHandler<>.HandleAsync));
                                        await (Task)handleMethod!.Invoke(handler, [message, stoppingToken])!;
                                    }
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