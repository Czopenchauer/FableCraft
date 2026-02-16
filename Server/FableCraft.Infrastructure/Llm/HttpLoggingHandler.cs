using Serilog;

namespace FableCraft.Infrastructure.Llm;

internal sealed class HttpLoggingHandler(ILogger logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];

        await LogRequest(request, requestId);

        var response = await base.SendAsync(request, cancellationToken);

        return response;
    }

    private async Task LogRequest(HttpRequestMessage request, string requestId)
    {
        if (request.Content == null)
        {
            logger.Information("[{RequestId}] Request: {Method} {Uri} (no body)",
                requestId,
                request.Method,
                request.RequestUri);
            return;
        }

        var content = await request.Content.ReadAsStringAsync();
        logger.Information("[{RequestId}] Request Body: {Body}", requestId, content);
    }
}