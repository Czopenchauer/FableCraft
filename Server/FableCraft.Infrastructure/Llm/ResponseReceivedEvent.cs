using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Queue;

using Microsoft.EntityFrameworkCore;

namespace FableCraft.Infrastructure.Llm;

public sealed class ResponseReceivedEvent : IMessage
{
    public Guid AdventureId { get; set; }

    public required string CallerName { get; init; }

    public required string RequestContent { get; init; }

    public required string ResponseContent { get; init; }

    public DateTimeOffset ReceivedAt { get; } = DateTimeOffset.UtcNow;

    public required int? InputToken { get; init; }

    public required int? OutputToken { get; init; }

    public required int? TotalToken { get; init; }

    public required long Duration { get; init; }
}

internal class ResponseReceivedEventHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IMessageHandler<ResponseReceivedEvent>
{
    public async Task HandleAsync(ResponseReceivedEvent message, CancellationToken cancellationToken)
    {
        var llmCallLog = new Persistence.Entities.LlmLog
        {
            AdventureId = message.AdventureId,
            CallerName = message.CallerName,
            RequestContent = message.RequestContent,
            ResponseContent = message.ResponseContent,
            ReceivedAt = message.ReceivedAt,
            InputToken = message.InputToken,
            OutputToken = message.OutputToken,
            TotalToken = message.TotalToken,
            Duration = message.Duration
        };
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.LlmCallLogs.Add(llmCallLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}