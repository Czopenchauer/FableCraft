using System.Reflection;
using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace FableCraft.Infrastructure.Queue;

internal class InMemoryMessageDispatcher : IMessageDispatcher
{
    private readonly Channel<IMessage> _channel;

    public InMemoryMessageDispatcher(Channel<IMessage> channel)
    {
        _channel = channel;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }
}

internal class InMemoryMessageReader : BackgroundService
{
    private readonly Channel<IMessage> _channel;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public InMemoryMessageReader(IServiceProvider serviceProvider, Channel<IMessage> channel, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Starting In-Memory Message Reader...");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (await _channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (_channel.Reader.TryRead(out IMessage? message))
                    {
                        ArgumentNullException.ThrowIfNull(message);
                        _ = Task.Run(async () =>
                            {
                                try
                                {
                                    Type messageType = message.GetType();
                                    Type handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
                                    await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                                    MethodInfo? handleMethod =
                                        handlerType.GetMethod(nameof(IMessageHandler<IMessage>.HandleAsync));
                                    await (Task)handleMethod!.Invoke(handler, [message, CancellationToken.None])!;
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error(ex, "Error processing message of type {MessageType}", message.GetType().Name);
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
            _logger.Error(ex, "Unhandled exception in In-Memory Message Reader");
        }
    }
}