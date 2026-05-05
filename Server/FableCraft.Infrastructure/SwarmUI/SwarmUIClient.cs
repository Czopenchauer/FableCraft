using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using FableCraft.Infrastructure.Images;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FableCraft.Infrastructure.SwarmUI;

/// <summary>
/// Client for the SwarmUI native Text2Image REST API.
/// Flow: GetNewSession (cached) → GenerateText2Image (synchronous) → optionally GET image bytes.
/// </summary>
public sealed class SwarmUIClient : IImageGenerationClient
{
    private const string DataUriPrefix = "data:image/";

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SwarmUISettings> _settingsMonitor;
    private readonly ILogger<SwarmUIClient> _logger;

    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private string? _sessionId;

    public SwarmUIClient(
        HttpClient httpClient,
        IOptionsMonitor<SwarmUISettings> settingsMonitor,
        ILogger<SwarmUIClient> logger)
    {
        _httpClient = httpClient;
        _settingsMonitor = settingsMonitor;
        _logger = logger;
    }

    private SwarmUISettings Settings => _settingsMonitor.CurrentValue;

    public async Task<ImageGenerationResult> GenerateImageAsync(
        string positivePrompt,
        string? negativePrompt,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var settings = Settings;
        var withAppendix = AppendIfPresent(positivePrompt, settings.PositiveAppendix);
        var withFace = AppendIfPresent(withAppendix, BuildSegmentFragment("face", settings.FaceRestorationPrompt));
        var finalPositive = AppendIfPresent(withFace, BuildSegmentFragment("hand", settings.HandRestorationPrompt));
        var finalNegative = AppendIfPresent(negativePrompt, settings.NegativeAppendix);

        var imageField = await SubmitWithSessionRetryAsync(finalPositive, finalNegative, cancellationToken);
        var imageBytes = await ResolveImageBytesAsync(imageField, cancellationToken);

        stopwatch.Stop();

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            GenerationDurationMs = stopwatch.ElapsedMilliseconds
        };
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sessionId = await EnsureSessionAsync(forceRefresh: false, cancellationToken);
            return !string.IsNullOrEmpty(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SwarmUI health check failed");
            return false;
        }
    }

    private static string AppendIfPresent(string? llmPrompt, string appendix)
    {
        var llm = llmPrompt ?? "";
        if (string.IsNullOrWhiteSpace(appendix)) return llm;
        if (string.IsNullOrWhiteSpace(llm)) return appendix;
        return $"{llm}, {appendix}";
    }

    private static string BuildSegmentFragment(string segmentClass, string subPrompt) =>
        string.IsNullOrWhiteSpace(subPrompt)
            ? ""
            : $"<segment:{segmentClass}>{subPrompt}</segment>";

    private (List<string>? loras, List<double>? weights) ResolveLoras(SwarmUISettings settings)
    {
        if (settings.Loras.Count == 0)
        {
            return (null, null);
        }

        if (settings.LoraWeights.Count == 0)
        {
            return (settings.Loras, null);
        }

        if (settings.LoraWeights.Count != settings.Loras.Count)
        {
            _logger.LogWarning(
                "SwarmUI Loras ({LoraCount}) and LoraWeights ({WeightCount}) lengths differ; ignoring weights.",
                settings.Loras.Count, settings.LoraWeights.Count);
            return (settings.Loras, null);
        }

        return (settings.Loras, settings.LoraWeights);
    }

    private async Task<string> SubmitWithSessionRetryAsync(
        string positive,
        string negative,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var sessionId = await EnsureSessionAsync(forceRefresh: attempt > 0, cancellationToken);

            var (loras, loraWeights) = ResolveLoras(Settings);

            var request = new GenerateText2ImageRequest
            {
                SessionId = sessionId,
                Images = 1,
                Prompt = positive,
                NegativePrompt = negative,
                Model = Settings.Model,
                Width = Settings.Width,
                Height = Settings.Height,
                Steps = Settings.Steps,
                CfgScale = Settings.CfgScale,
                Seed = Settings.Seed,
                Sampler = Settings.Sampler,
                Loras = loras,
                LoraWeights = loraWeights
            };

            using var timeoutCts = new CancellationTokenSource(Settings.GenerationTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var url = $"{Settings.BaseUrl}/API/GenerateText2Image";
            var requestJson = JsonSerializer.Serialize(request);
            _logger.LogInformation(
                "SwarmUI: submitting GenerateText2Image to {Url} (model={Model}, size={Width}x{Height}, steps={Steps}, cfg={Cfg}, loras={LoraCount})",
                url, request.Model, request.Width, request.Height, request.Steps, request.CfgScale,
                request.Loras?.Count ?? 0);
            _logger.LogDebug("SwarmUI request body: {RequestJson}", requestJson);

            using var generateContent = BuildJsonContent(requestJson);
            var response = await _httpClient.PostAsync(url, generateContent, linkedCts.Token);
            var rawBody = await response.Content.ReadAsStringAsync(linkedCts.Token);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogInformation("SwarmUI session expired (401); refreshing. Body: {Body}", rawBody);
                InvalidateSession();
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "SwarmUI GenerateText2Image failed: {StatusCode} {ReasonPhrase}. Response body: {Body}. Request body: {RequestJson}",
                    (int)response.StatusCode, response.ReasonPhrase, rawBody, requestJson);
                throw new ImageGenerationException(
                    $"SwarmUI GenerateText2Image returned {(int)response.StatusCode} {response.ReasonPhrase}: {rawBody}");
            }

            _logger.LogDebug("SwarmUI GenerateText2Image response: {Body}", Truncate(rawBody, 2000));

            GenerateText2ImageResponse? body;
            try
            {
                body = JsonSerializer.Deserialize<GenerateText2ImageResponse>(rawBody);
            }
            catch (JsonException jx)
            {
                throw new ImageGenerationException(
                    $"SwarmUI GenerateText2Image returned non-JSON body: {Truncate(rawBody, 2000)}", jx);
            }

            if (body?.Error is { } error)
            {
                if (IsInvalidSession(error, body.ErrorId))
                {
                    _logger.LogInformation("SwarmUI reported invalid_session_id; refreshing. Body: {Body}", rawBody);
                    InvalidateSession();
                    continue;
                }

                _logger.LogError("SwarmUI generation error. ErrorId={ErrorId}, Error={Error}, Body={Body}",
                    body.ErrorId, error, rawBody);
                throw new ImageGenerationException($"SwarmUI generation failed (errorId={body.ErrorId}): {error}");
            }

            if (body?.Images is not { Count: > 0 } images)
            {
                _logger.LogError("SwarmUI returned no images. Body: {Body}", rawBody);
                throw new ImageGenerationException($"SwarmUI returned no images. Body: {Truncate(rawBody, 1000)}");
            }

            return images[0];
        }

        throw new ImageGenerationException("SwarmUI generation failed after session refresh.");
    }

    private static StringContent BuildJsonContent(string json)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        // Force Content-Length by materializing the byte array; some servers
        // (SwarmUI) reject requests without it.
        content.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);
        return content;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + $"... [truncated, {s.Length - max} more chars]";

    private static bool IsInvalidSession(string error, string? errorId) =>
        (errorId?.Contains("invalid_session", StringComparison.OrdinalIgnoreCase) ?? false) ||
        error.Contains("invalid_session", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("session", StringComparison.OrdinalIgnoreCase) && error.Contains("invalid", StringComparison.OrdinalIgnoreCase);

    private async Task<string> EnsureSessionAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _sessionId is { } cached)
        {
            return cached;
        }

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && _sessionId is { } cachedAfterLock)
            {
                return cachedAfterLock;
            }

            var url = $"{Settings.BaseUrl}/API/GetNewSession";
            _logger.LogInformation("SwarmUI: requesting new session from {Url}", url);

            // SwarmUI rejects requests without an explicit Content-Length header
            // ("error_id": "basic_api", "error": "bad content length"). PostAsJsonAsync
            // can stream with chunked encoding, so build a StringContent manually so
            // the framework sets Content-Length from the byte count.
            using var sessionContent = BuildJsonContent("{}");
            var response = await _httpClient.PostAsync(url, sessionContent, cancellationToken);
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "SwarmUI GetNewSession failed: {StatusCode} {ReasonPhrase}. Response body: {Body}. Request URL: {Url}",
                    (int)response.StatusCode, response.ReasonPhrase, rawBody, url);
                throw new ImageGenerationException(
                    $"SwarmUI GetNewSession returned {(int)response.StatusCode} {response.ReasonPhrase}: {rawBody}");
            }

            _logger.LogDebug("SwarmUI GetNewSession response: {Body}", rawBody);

            NewSessionResponse? body;
            try
            {
                body = JsonSerializer.Deserialize<NewSessionResponse>(rawBody);
            }
            catch (JsonException jx)
            {
                throw new ImageGenerationException(
                    $"SwarmUI GetNewSession returned non-JSON body: {rawBody}", jx);
            }

            if (string.IsNullOrEmpty(body?.SessionId))
            {
                throw new ImageGenerationException(
                    $"SwarmUI did not return a session_id. Body: {rawBody}");
            }

            _sessionId = body.SessionId;
            return _sessionId;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private void InvalidateSession()
    {
        _sessionId = null;
    }

    private async Task<byte[]> ResolveImageBytesAsync(string imageField, CancellationToken cancellationToken)
    {
        if (imageField.StartsWith(DataUriPrefix, StringComparison.Ordinal))
        {
            var commaIdx = imageField.IndexOf(',');
            if (commaIdx < 0)
            {
                throw new ImageGenerationException("SwarmUI returned a malformed data URI.");
            }
            var base64 = imageField[(commaIdx + 1)..];
            return Convert.FromBase64String(base64);
        }

        var url = imageField.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? imageField
            : $"{Settings.BaseUrl.TrimEnd('/')}/{imageField.TrimStart('/')}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
