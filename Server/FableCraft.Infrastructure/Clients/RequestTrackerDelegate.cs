using FableCraft.Infrastructure.Docker;

namespace FableCraft.Infrastructure.Clients;

internal sealed class RequestTrackerDelegate(IContainerMonitor containerMonitor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            containerMonitor.Increment(ProcessExecutionContext.AdventureId.Value);
            return base.SendAsync(request, cancellationToken);
        }
        finally
        {
            containerMonitor.Decrement(ProcessExecutionContext.AdventureId.Value);
        }
    }
}