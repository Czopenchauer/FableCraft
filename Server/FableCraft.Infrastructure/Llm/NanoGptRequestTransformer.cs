using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Serilog;

namespace FableCraft.Infrastructure.Llm;

internal sealed class NanoGptRequestTransformer(ILogger logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];

        await TransformRequest(request, requestId);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task TransformRequest(HttpRequestMessage request, string requestId)
    {
        if (request.Content == null)
        {
            return;
        }

        var content = await request.Content.ReadAsStringAsync();

        if (content.Contains("max_completion_tokens"))
        {
            try
            {
                var jsonNode = JsonNode.Parse(content);
                if (jsonNode is JsonObject jsonObject && jsonObject.ContainsKey("max_completion_tokens"))
                {
                    var maxTokens = jsonObject["max_completion_tokens"];
                    jsonObject.Remove("max_completion_tokens");
                    jsonObject["max_tokens"] = maxTokens?.DeepClone();

                    content = jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                }
            }
            catch (JsonException ex)
            {
                logger.Warning(ex, "[{RequestId}] Failed to transform request body", requestId);
            }
        }

        if (content.Contains("glm", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var jsonNode = JsonNode.Parse(content);
                if (jsonNode is JsonObject jsonObject)
                {
                    jsonObject["thinking"] = new JsonObject
                    {
                        ["type"] = "enabled"
                    };

                    content = jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");
                }
            }
            catch (JsonException ex)
            {
                logger.Warning(ex, "[{RequestId}] Failed to add thinking to request body", requestId);
            }
        }
    }
}
