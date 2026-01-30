using FableCraft.Infrastructure.Docker;

namespace FableCraft.Infrastructure.Clients;

internal sealed class RequestTrackerDelegate(IContainerMonitor containerMonitor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var adventureId = ProcessExecutionContext.AdventureId.Value;
        try
        {
            containerMonitor.Increment(adventureId);
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            containerMonitor.Decrement(adventureId);
        }
    }
}