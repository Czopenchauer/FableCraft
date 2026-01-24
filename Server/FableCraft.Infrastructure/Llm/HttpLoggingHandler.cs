using Serilog;

namespace FableCraft.Infrastructure.Llm;

internal sealed class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public HttpLoggingHandler(ILogger logger)
    {
        _logger = logger;
    }

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
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            _logger.Information("[{RequestId}] Request Body: {Body}", requestId, content);
        }
    }
}