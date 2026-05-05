namespace FableCraft.Infrastructure.Images;

public interface IImageGenerationClient
{
    Task<ImageGenerationResult> GenerateImageAsync(
        string positivePrompt,
        string? negativePrompt,
        CancellationToken cancellationToken);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}

public sealed class ImageGenerationResult
{
    public required byte[] ImageBytes { get; init; }
    public long GenerationDurationMs { get; init; }
}

public class ImageGenerationException : Exception
{
    public ImageGenerationException(string message) : base(message) { }
    public ImageGenerationException(string message, Exception innerException) : base(message, innerException) { }
}
